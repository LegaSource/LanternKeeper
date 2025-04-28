using GameNetcodeStuff;
using LanternKeeper.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Behaviours;

public class LanternKeeperAI : EnemyAI
{
    public float angerMeter = 1f;
    public Lantern lastLanternLit;

    public Transform TurnCompass;
    public AudioClip[] CrawlSounds = Array.Empty<AudioClip>();
    public AudioClip BiteSound;
    public float crawlTimer = 0f;

    public List<EntranceTeleport> entrances;

    public Coroutine getUpCoroutine;
    public Coroutine damagePlayerCoroutine;
    public Coroutine poisonPlayerCoroutine;
    public ParticleSystem poisonParticle;

    public enum State
    {
        WANDERING,
        CHASING,
        ATTACKING
    }

    public override void Start()
    {
        base.Start();

        currentBehaviourStateIndex = (int)State.WANDERING;
        creatureAnimator.SetTrigger("startMove");
        StartSearch(transform.position);
    }

    public override void Update()
    {
        base.Update();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        creatureAnimator.SetBool("stunned", stunNormalizedTimer > 0f);
        if (stunNormalizedTimer > 0f)
        {
            agent.speed = 0f;
            if (stunnedByPlayer != null)
            {
                targetPlayer = stunnedByPlayer;
                StopSearch(currentSearch);
                SwitchToBehaviourClientRpc((int)State.CHASING);
            }
            return;
        }
        PlayCrawlSound();
        int state = currentBehaviourStateIndex;
        if (targetPlayer != null && (state == (int)State.CHASING || state == (int)State.ATTACKING))
        {
            TurnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, TurnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
        }
    }

    public void PlayCrawlSound()
    {
        if (currentBehaviourStateIndex == (int)State.ATTACKING) return;

        crawlTimer -= Time.deltaTime;
        if (CrawlSounds.Length > 0 && crawlTimer <= 0)
        {
            creatureSFX.PlayOneShot(CrawlSounds[UnityEngine.Random.Range(0, CrawlSounds.Length)]);
            crawlTimer = currentBehaviourStateIndex == (int)State.WANDERING ? 1.3f : 1.1f;
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.WANDERING:
                agent.speed = 1.5f * angerMeter;
                if (FoundClosestPlayerInRange(25, 10))
                {
                    StopSearch(currentSearch);
                    DoAnimationClientRpc("startChase");
                    SwitchToBehaviourClientRpc((int)State.CHASING);
                    return;
                }
                break;
            case (int)State.CHASING:
                agent.speed = 3f * angerMeter;
                if (TargetOutsideChasedPlayer()) return;
                if (!TargetClosestPlayerInAnyCase() || (Vector3.Distance(transform.position, targetPlayer.transform.position) > 20f && !CheckLineOfSightForPosition(targetPlayer.transform.position)))
                {
                    StartSearch(transform.position);
                    DoAnimationClientRpc("startMove");
                    SwitchToBehaviourClientRpc((int)State.WANDERING);
                    return;
                }
                SetMovingTowardsTargetPlayer(targetPlayer);
                break;
            case (int)State.ATTACKING:
                agent.speed = CollidesWithEnemy(targetPlayer.gameplayCamera.transform.position) ? 0f : 1f * angerMeter;
                if (damagePlayerCoroutine == null)
                {
                    if (targetPlayer == null || Vector3.Distance(transform.position, targetPlayer.transform.position) > 5f)
                    {
                        StartSearch(transform.position);
                        DoAnimationClientRpc("startGetDown");
                        DoAnimationClientRpc("startChase");
                        SwitchToBehaviourClientRpc((int)State.CHASING);
                        return;
                    }
                    damagePlayerCoroutine = StartCoroutine(DamagePlayerCoroutine(targetPlayer));
                }
                SetMovingTowardsTargetPlayer(targetPlayer);
                break;

            default:
                break;
        }
    }

    private bool FoundClosestPlayerInRange(int range, int senseRange)
    {
        PlayerControllerB player = CheckLineOfSightForPlayer(60f, range, senseRange);
        return player != null && PlayerIsTargetable(player) && (bool)(targetPlayer = player);
    }

    public bool TargetOutsideChasedPlayer()
    {
        if (targetPlayer.isInsideFactory == isOutside)
        {
            GoTowardsEntrance();
            return true;
        }
        return false;
    }

    public bool TargetClosestPlayerInAnyCase()
    {
        mostOptimalDistance = 2000f;
        targetPlayer = null;
        for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
        {
            tempDist = Vector3.Distance(transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
            if (tempDist < mostOptimalDistance)
            {
                mostOptimalDistance = tempDist;
                targetPlayer = StartOfRound.Instance.allPlayerScripts[i];
            }
        }
        return targetPlayer != null;
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);

        if (currentBehaviourStateIndex != (int)State.CHASING) return;

        PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
        if (player == null) return;

        getUpCoroutine ??= StartCoroutine(GetUpCoroutine());
    }

    public IEnumerator GetUpCoroutine()
    {
        DoAnimationServerRpc("startGetUp");
        DoAnimationServerRpc("startIdle");

        yield return new WaitForSeconds(0.75f);

        SwitchToBehaviourServerRpc((int)State.ATTACKING);
        getUpCoroutine = null;
    }

    public IEnumerator DamagePlayerCoroutine(PlayerControllerB player)
    {
        DoAnimationClientRpc("startBite");
        PlayBiteClientRpc();

        yield return new WaitForSeconds(1f);

        if (CheckLineOfSightForPosition(player.gameplayCamera.transform.position, 70f, 20, 1f)
            || CollidesWithEnemy(player.gameplayCamera.transform.position))
        {
            DamagePlayerClientRpc((int)player.playerClientId);
        }
        DoAnimationClientRpc("startIdle");

        yield return new WaitForSeconds(1.5f);

        damagePlayerCoroutine = null;
    }

    [ClientRpc]
    public void PlayBiteClientRpc()
        => creatureSFX.PlayOneShot(BiteSound);

    public bool CollidesWithEnemy(Vector3 position)
    {
        Collider[] hitColliders = Physics.OverlapSphere(position, 1f, 524288, QueryTriggerInteraction.Collide);
        foreach (Collider hitCollider in hitColliders)
        {
            EnemyAI enemy = hitCollider.GetComponent<EnemyAICollisionDetect>()?.mainScript;
            if (enemy != null && enemy == this) return true;
        }
        return false;
    }

    [ClientRpc]
    public void DamagePlayerClientRpc(int playerId)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        if (player == GameNetworkManager.Instance.localPlayerController)
        {
            player.DamagePlayer((int)(20 * angerMeter), hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling);

            StopPoisonParticlePlayer();
            poisonPlayerCoroutine = StartCoroutine(PoisonPlayerCoroutine(player));
        }
    }

    public IEnumerator PoisonPlayerCoroutine(PlayerControllerB player)
    {
        poisonParticle = LKUtilities.SpawnPoisonParticle(player.transform);

        float timePassed = 0f;
        while (timePassed < ConfigManager.enemyPoisonDuration.Value)
        {
            HUDManager.Instance.drunknessFilter.weight = Mathf.Max(ConfigManager.enemyPoisonIntensity.Value, HUDManager.Instance.drunknessFilter.weight);

            if (Mathf.FloorToInt(timePassed * 10) % 10 == 0)
                player.DamagePlayer(ConfigManager.enemyPoisonDamage.Value, hasDamageSFX: true, callRPC: true, CauseOfDeath.Suffocation);

            timePassed += Time.deltaTime;

            yield return null;
        }

        Destroy(poisonParticle.gameObject);
        poisonPlayerCoroutine = null;
    }

    public void StopPoisonParticlePlayer()
    {
        if (poisonPlayerCoroutine != null) StopCoroutine(poisonPlayerCoroutine);
        if (poisonParticle != null) Destroy(poisonParticle.gameObject);
        poisonPlayerCoroutine = null;
    }

    public void GoTowardsEntrance()
    {
        if (entrances != null || entrances.Count == 0)
            entrances = FindObjectsOfType<EntranceTeleport>().ToList();

        EntranceTeleport entranceTeleport = entrances.Where(e => e.isEntranceToBuilding == isOutside)
            .OrderBy(e => Vector3.Distance(transform.position, e.entrancePoint.position))
            .FirstOrDefault();

        if (Vector3.Distance(transform.position, entranceTeleport.entrancePoint.position) < 1f)
        {
            Vector3 exitPosition = entrances.FirstOrDefault(e => e.isEntranceToBuilding != entranceTeleport.isEntranceToBuilding && e.entranceId == entranceTeleport.entranceId)
                .entrancePoint
                .position;
            _ = StartCoroutine(TeleportEnemyCoroutine(exitPosition, !isOutside));
            return;
        }

        _ = SetDestinationToPosition(entranceTeleport.entrancePoint.position);
    }

    public IEnumerator TeleportEnemyCoroutine(Vector3 position, bool isOutside)
    {
        yield return new WaitForSeconds(1f);
        TeleportEnemyClientRpc(position, isOutside);
    }

    [ClientRpc]
    public void TeleportEnemyClientRpc(Vector3 teleportPosition, bool isOutside)
    {
        SetEnemyOutside(isOutside);
        serverPosition = teleportPosition;
        transform.position = teleportPosition;
        _ = agent.Warp(teleportPosition);
        SyncPositionToClients();
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (isEnemyDead) return;

        if ((IsServer || IsHost) && !enemyType.canDie)
        {
            _ = LKUtilities.SpawnItem(LanternKeeper.fortuneCookieObj, transform.position + (Vector3.up * 0.5f), !isOutside);
            return;
        }

        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);

        SetEnemyStunned(setToStunned: true, 0.2f, playerWhoHit);
        enemyHP -= force;
        if (enemyHP <= 0 && IsOwner) KillEnemyOnOwnerClient();
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy();

        if (damagePlayerCoroutine != null) StopCoroutine(damagePlayerCoroutine);

        if (IsServer || IsHost)
        {
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
                SpawnPoisonDagger(LanternKeeper.daggerObj, transform.position + (Vector3.up * 0.5f));
        }
    }

    public void SpawnPoisonDagger(GameObject spawnPrefab, Vector3 position)
    {
        GameObject gameObject = Instantiate(spawnPrefab, position, Quaternion.identity, StartOfRound.Instance.propsContainer);
        GrabbableObject grabbableObject = gameObject.GetComponent<GrabbableObject>();
        grabbableObject.fallTime = 0f;
        grabbableObject.isInFactory = !isOutside;
        gameObject.GetComponent<NetworkObject>().Spawn();

        SetScrapValueClientRpc(grabbableObject.GetComponent<NetworkObject>(), new System.Random().Next(ConfigManager.daggerMinValue.Value, ConfigManager.daggerMaxValue.Value));
    }

    [ClientRpc]
    public void SetScrapValueClientRpc(NetworkObjectReference obj, int scrapValue)
    {
        if (obj.TryGet(out NetworkObject networkObject))
        {
            GrabbableObject grabbableObject = networkObject.gameObject.GetComponentInChildren<GrabbableObject>();
            grabbableObject.SetScrapValue(scrapValue);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DoAnimationServerRpc(string animationState)
        => DoAnimationClientRpc(animationState);

    [ClientRpc]
    public void DoAnimationClientRpc(string animationState)
        => creatureAnimator.SetTrigger(animationState);
}
