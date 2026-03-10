using GameNetcodeStuff;
using LanternKeeper.Managers;
using LegaFusionCore.Managers;
using LegaFusionCore.Managers.NetworkManagers;
using LegaFusionCore.Registries;
using LegaFusionCore.Utilities;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Behaviours;

public class LanternKeeperAI : EnemyAI
{
    public Transform TurnCompass;
    public AudioClip[] CrawlSounds = [];
    public AudioClip BiteSound;
    public Transform ThrowPoint;

    public float crawlTimer = 0f;
    public float throwTimer = 0f;
    public float throwCooldown = 5f;

    public bool canThrow = false;

    public Coroutine stunCoroutine;
    public Coroutine getUpCoroutine;
    public Coroutine getDownCoroutine;
    public Coroutine throwCoroutine;
    public Coroutine biteCoroutine;

    public enum State { WANDERING, CHASING, THROWING }

    public override void Start()
    {
        base.Start();

        currentBehaviourStateIndex = (int)State.WANDERING;
        StartSearch(transform.position);
        SpawnLanternsForServer();
    }

    public void SpawnLanternsForServer()
        => LFCMapObjectsManager.SpawnScatteredMapObjectsForServer(mapObjectsAmount: 2,
            minInside: 1,
            minOutside: 1,
            spawnAction: SpawnLanternForServer);

    public void SpawnLanternForServer(Vector3 position, bool isOutside)
    {
        GameObject gameObject = Instantiate(LanternKeeper.lanternObj, position + (Vector3.down * 0.5f), Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
        Lantern lantern = gameObject.GetComponent<Lantern>();
        if (isOutside) lantern.transform.localScale *= 2f;
        gameObject.GetComponent<NetworkObject>().Spawn(true);
        lantern.InitializeLanternEveryoneRpc(thisNetworkObject, isOutside);
    }

    public override void Update()
    {
        base.Update();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        PlayCrawlSound();
        int state = currentBehaviourStateIndex;
        if (targetPlayer != null && (state == (int)State.CHASING || state == (int)State.THROWING))
        {
            TurnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, TurnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
        }
        LFCUtilities.UpdateTimer(ref throwTimer, throwCooldown, !canThrow, () => canThrow = true);
    }

    public void PlayCrawlSound()
    {
        if (currentBehaviourStateIndex == (int)State.THROWING) return;

        crawlTimer -= Time.deltaTime;
        if (CrawlSounds.Length > 0 && crawlTimer <= 0)
        {
            creatureSFX.PlayOneShot(CrawlSounds[Random.Range(0, CrawlSounds.Length)]);
            crawlTimer = currentBehaviourStateIndex == (int)State.WANDERING ? 1.3f : 1.1f;
        }
    }

    public override void SetEnemyStunned(bool setToStunned, float setToStunTime = 1.34f, PlayerControllerB setStunnedByPlayer = null)
    {
        if (LFCUtilities.IsServer && setToStunned && stunCoroutine == null)
        {
            base.SetEnemyStunned(setToStunned, setToStunTime, setStunnedByPlayer);
            stunCoroutine = StartCoroutine(StunCoroutine());
        }
    }

    public IEnumerator StunCoroutine()
    {
        CancelGetUpCoroutine();
        CancelGetDownCoroutine();
        CancelThrowCoroutine();
        CancelBiteCoroutine();

        agent.speed = 0f;
        DoAnimationEveryoneRpc("startStun");
        yield return this.WaitForFullAnimation("stun");

        while (stunNormalizedTimer > 0f) yield return null;
        while (postStunInvincibilityTimer > 0f) yield return null;

        if (currentBehaviourStateIndex == (int)State.THROWING)
        {
            DoAnimationEveryoneRpc("startGetDown");
            yield return this.WaitForFullAnimation("getDown");
        }

        DoAnimationEveryoneRpc("startMove");
        if (currentBehaviourStateIndex == (int)State.WANDERING && stunnedByPlayer != null)
        {
            targetPlayer = stunnedByPlayer;
            StopSearch(currentSearch);
            SwitchToBehaviourClientRpc((int)State.CHASING);
        }

        stunCoroutine = null;
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.WANDERING: DoWandering(); break;
            case (int)State.CHASING: DoChasing(); break;
            case (int)State.THROWING: DoThrowing(); break;
        }
    }

    public void DoWandering()
    {
        agent.speed = 3f;
        if (this.FoundClosestPlayerInRange(25, 10))
        {
            StopSearch(currentSearch);
            SwitchToBehaviourClientRpc((int)State.CHASING);
        }
    }

    public void DoChasing()
    {
        if (biteCoroutine != null || getUpCoroutine != null || getDownCoroutine != null) return;

        agent.speed = 6f;
        if (this.TargetOutsideChasedPlayer()) return;
        if (!this.TargetClosestPlayerInAnyCase(out float distanceWithPlayer) || (distanceWithPlayer > 25f && !CheckLineOfSightForPosition(targetPlayer.transform.position)))
        {
            StartSearch(transform.position);
            SwitchToBehaviourClientRpc((int)State.WANDERING);
            return;
        }
        if (CanThrow() && distanceWithPlayer <= 15f && (distanceWithPlayer <= 2f || CheckLineOfSightForPosition(targetPlayer.transform.position)))
        {
            getUpCoroutine = StartCoroutine(GetUpCoroutine());
            SwitchToBehaviourServerRpc((int)State.THROWING);
            return;
        }
        SetMovingTowardsTargetPlayer(targetPlayer);
    }

    public IEnumerator GetUpCoroutine()
    {
        agent.speed = 0f;
        DoAnimationEveryoneRpc("startGetUp");
        yield return this.WaitForFullAnimation("getup");

        DoAnimationEveryoneRpc("startIdle");
        getUpCoroutine = null;
    }

    public void CancelGetUpCoroutine()
    {
        if (getUpCoroutine != null)
        {
            StopCoroutine(getUpCoroutine);
            getUpCoroutine = null;
        }
    }

    public void DoThrowing()
    {
        if (throwCoroutine != null || getUpCoroutine != null || getDownCoroutine != null) return;

        agent.speed = 0f;
        float distanceWithPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (!CanThrow() || distanceWithPlayer > 20f || (distanceWithPlayer > 2f && !CheckLineOfSightForPosition(targetPlayer.transform.position)))
        {
            getDownCoroutine = StartCoroutine(GetDownCoroutine());
            SwitchToBehaviourServerRpc((int)State.CHASING);
            return;
        }
        throwCoroutine = StartCoroutine(ThrowCoroutine());
    }

    public IEnumerator GetDownCoroutine()
    {
        agent.speed = 0f;
        DoAnimationEveryoneRpc("startGetDown");
        yield return this.WaitForFullAnimation("getdown");

        DoAnimationEveryoneRpc("startMove");
        getDownCoroutine = null;
    }

    public void CancelGetDownCoroutine()
    {
        if (getDownCoroutine != null)
        {
            StopCoroutine(getDownCoroutine);
            getDownCoroutine = null;
        }
    }

    public IEnumerator ThrowCoroutine()
    {
        canThrow = false;
        DoAnimationEveryoneRpc("startBite");
        PlayBiteEveryoneRpc();

        GameObject gameObject = Instantiate(LanternKeeper.poisonBallObj, ThrowPoint.transform.position, Quaternion.identity);
        gameObject.GetComponent<NetworkObject>().Spawn();
        gameObject.GetComponent<PoisonBall>().ThrowFromPositionEveryoneRpc(entityId: NetworkObjectId,
            startPosition: ThrowPoint.transform.position,
            direction: targetPlayer.transform.position + (Vector3.up * 1.5f) - ThrowPoint.transform.position,
            isOutside: isOutside);

        yield return this.WaitForFullAnimation("bite");
        DoAnimationEveryoneRpc("startIdle");

        throwCoroutine = null;
    }

    public void CancelThrowCoroutine()
    {
        if (throwCoroutine != null)
        {
            StopCoroutine(throwCoroutine);
            throwCoroutine = null;
        }
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);

        if (currentBehaviourStateIndex != (int)State.CHASING) return;
        PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
        if (!LFCUtilities.ShouldBeLocalPlayer(player)) return;

        BiteServerRpc((int)player.playerClientId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void BiteServerRpc(int playerId)
    {
        if (currentBehaviourStateIndex == (int)State.CHASING && biteCoroutine == null && getUpCoroutine == null && getDownCoroutine == null)
            biteCoroutine = StartCoroutine(BiteCoroutine(StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>()));
    }

    public IEnumerator BiteCoroutine(PlayerControllerB player)
    {
        yield return GetUpCoroutine();
        DoAnimationEveryoneRpc("startBite");
        PlayBiteEveryoneRpc();

        yield return this.WaitForFullAnimation("bite");
        LFCNetworkManager.Instance.DamagePlayerEveryoneRpc((int)player.playerClientId, ConfigManager.enemyDirectDamage.Value, hasDamageSFX: true, callRPC: true, (int)CauseOfDeath.Mauling);

        DoAnimationEveryoneRpc("startIdle");
        yield return GetDownCoroutine();

        biteCoroutine = null;
    }

    public void CancelBiteCoroutine()
    {
        if (biteCoroutine != null)
        {
            StopCoroutine(biteCoroutine);
            biteCoroutine = null;
        }
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (!isEnemyDead)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            enemyHP -= force;
            if (enemyHP <= 0 && IsOwner) KillEnemyOnOwnerClient();
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy();

        if (LFCUtilities.IsServer)
        {
            CancelGetUpCoroutine();
            CancelThrowCoroutine();
            CancelBiteCoroutine();

            PoisonDagger poisonDagger = LFCObjectsManager.SpawnObjectForServer(LanternKeeper.daggerObj, transform.position + (Vector3.up * 0.5f)) as PoisonDagger;
            poisonDagger.InitializeForServer();
        }
    }

    public bool CanThrow() => canThrow && targetPlayer != null && !LFCStatusEffectRegistry.HasStatus(targetPlayer.gameObject, LFCStatusEffectRegistry.StatusEffectType.POISON);

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void PlayBiteEveryoneRpc() => creatureSFX.PlayOneShot(BiteSound);

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void DoAnimationEveryoneRpc(string animationState) => creatureAnimator.SetTrigger(animationState);
}
