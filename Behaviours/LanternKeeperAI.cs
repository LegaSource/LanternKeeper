using GameNetcodeStuff;
using LanternKeeper.Managers;
using LegaFusionCore.Managers;
using LegaFusionCore.Registries;
using LegaFusionCore.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Behaviours;

public class LanternKeeperAI : EnemyAI
{
    public Transform TurnCompass;
    public AudioClip[] CrawlSounds = [];
    public AudioClip BiteSound;
    public float crawlTimer = 0f;

    public Coroutine getUpCoroutine;
    public Coroutine damagePlayerCoroutine;

    public enum State
    {
        WANDERING,
        CHASING,
        ATTACKING
    }

    public override void Start()
    {
        base.Start();

        if (LFCUtilities.IsServer) SpawnLanterns();
        currentBehaviourStateIndex = (int)State.WANDERING;
        creatureAnimator.SetTrigger("startMove");
        StartSearch(transform.position);
    }

    public void SpawnLanterns()
    {
        const float minDistance = 50f;
        List<Vector3> selectedPositions = [];
        StartOfRound.Instance.allPlayerScripts.Where(p => !p.isPlayerDead).ToList().ForEach(p => selectedPositions.Add(p.transform.position));

        LFCUtilities.Shuffle(RoundManager.Instance.outsideAINodes);
        LFCUtilities.Shuffle(RoundManager.Instance.insideAINodes);

        for (int i = 0; i < 2; i++)
        {
            float maxDistance = float.MinValue;
            Vector3 bestPosition = Vector3.zero;
            GameObject lastNodeSaved = null;

            // Déterminer si cette lanterne est à l'extérieur ou à l'intérieur
            bool isOutside = new System.Random().Next(0, 2) == 1;
            List<GameObject> nodes = (isOutside ? RoundManager.Instance.outsideAINodes : RoundManager.Instance.insideAINodes).ToList();
            float radius = isOutside ? 10f : 2f;

            foreach (GameObject node in nodes)
            {
                Vector3 candidatePosition = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(node.transform.position, radius, default, new System.Random()) + Vector3.up;
                if (!Physics.Raycast(candidatePosition, Vector3.down, out UnityEngine.RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) continue;

                Vector3 validPosition = hit.point;

                // Calculer la distance minimale avec les positions sélectionnées
                float minDistanceToSelected = selectedPositions.Count > 0
                    ? selectedPositions.Min(p => Vector3.Distance(p, validPosition))
                    : float.MaxValue;

                // Garder la position la plus éloignée des autres sélectionnées
                if (minDistanceToSelected > minDistance || minDistanceToSelected > maxDistance)
                {
                    maxDistance = minDistanceToSelected;
                    bestPosition = validPosition;
                    lastNodeSaved = node;

                    if (minDistanceToSelected > minDistance) break;
                }
            }

            if (bestPosition != Vector3.zero)
            {
                selectedPositions.Add(bestPosition);
                _ = nodes.Remove(lastNodeSaved);

                GameObject gameObject = Instantiate(LanternKeeper.lanternObj, bestPosition + (Vector3.down * 0.5f), Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
                Lantern lantern = gameObject.GetComponent<Lantern>();

                if (isOutside) lantern.transform.localScale *= 2f;

                gameObject.GetComponent<NetworkObject>().Spawn(true);
                lantern.InitializeLanternClientRpc(thisNetworkObject, isOutside);
            }
        }
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
                agent.speed = 3f;
                if (FoundClosestPlayerInRange(25, 10))
                {
                    StopSearch(currentSearch);
                    DoAnimationClientRpc("startChase");
                    SwitchToBehaviourClientRpc((int)State.CHASING);
                    return;
                }
                break;
            case (int)State.CHASING:
                agent.speed = 6f;
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
                agent.speed = 0f;
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

        if (currentBehaviourStateIndex != (int)State.CHASING || getUpCoroutine != null) return;

        PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
        if (player == null) return;

        getUpCoroutine = StartCoroutine(GetUpCoroutine());
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

        if (CheckLineOfSightForPosition(player.gameplayCamera.transform.position, 70f, 20, 4f)) DamagePlayerClientRpc((int)player.playerClientId);
        DoAnimationClientRpc("startIdle");

        yield return new WaitForSeconds(1.5f);

        damagePlayerCoroutine = null;
    }

    [ClientRpc]
    public void PlayBiteClientRpc() => creatureSFX.PlayOneShot(BiteSound);

    [ClientRpc]
    public void DamagePlayerClientRpc(int playerId)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        if (player == GameNetworkManager.Instance.localPlayerController)
            player.DamagePlayer(ConfigManager.enemyDirectDamage.Value, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling);
        LFCStatusEffectRegistry.ApplyStatus(player.gameObject, LFCStatusEffectRegistry.StatusEffectType.POISON, playerWhoHit: -1, ConfigManager.enemyPoisonDuration.Value, ConfigManager.enemyPoisonDamage.Value);
    }

    public void GoTowardsEntrance()
    {
        EntranceTeleport entranceTeleport = LFCSpawnRegistry.GetAllAs<EntranceTeleport>()?
            .Where(e => e.isEntranceToBuilding == isOutside)
            .OrderBy(e => Vector3.Distance(transform.position, e.entrancePoint.position))
            .FirstOrDefault();
        if (entranceTeleport == null) return;

        if (Vector3.Distance(transform.position, entranceTeleport.entrancePoint.position) < 1f)
        {
            Vector3 exitPosition = LFCSpawnRegistry.GetAllAs<EntranceTeleport>()
                .FirstOrDefault(e => e.isEntranceToBuilding != entranceTeleport.isEntranceToBuilding && e.entranceId == entranceTeleport.entranceId)
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

        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);

        SetEnemyStunned(setToStunned: true, 0.2f, playerWhoHit);
        enemyHP -= force;
        if (enemyHP <= 0 && IsOwner) KillEnemyOnOwnerClient();
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy();

        if (damagePlayerCoroutine != null) StopCoroutine(damagePlayerCoroutine);
        if (LFCUtilities.IsServer)
        {
            PoisonDagger poisonDagger = LFCObjectsManager.SpawnObjectForServer(LanternKeeper.daggerObj, transform.position + (Vector3.up * 0.5f)) as PoisonDagger;
            poisonDagger.InitializeForServer();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DoAnimationServerRpc(string animationState)
        => DoAnimationClientRpc(animationState);

    [ClientRpc]
    public void DoAnimationClientRpc(string animationState)
        => creatureAnimator.SetTrigger(animationState);
}
