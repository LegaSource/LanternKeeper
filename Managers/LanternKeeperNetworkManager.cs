using GameNetcodeStuff;
using LegaFusionCore.Managers;
using LethalStatus.StatusEffects;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Managers;

public class LanternKeeperNetworkManager : NetworkBehaviour
{
    public static LanternKeeperNetworkManager Instance;

    public void Awake() => Instance = this;

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SpawnPoisonMarkEveryoneRpc(int playerId, int playerWhoHit)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        LFCGlobalManager.PlayParticle(LanternKeeper.poisonMarkObj, player.transform.position, player.transform.rotation);
        LSStatusEffectRegistry.ApplyStatus(player.gameObject, LSStatusEffectRegistry.StatusEffectType.POISON, playerWhoHit, 10, 100);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SpawnPoisonMarkEveryoneRpc(NetworkObjectReference obj, int playerWhoHit)
    {
        if (obj.TryGet(out NetworkObject networkObject))
        {
            EnemyAI enemy = networkObject.gameObject.GetComponent<EnemyAI>();
            Vector3 size = enemy.GetComponentInChildren<BoxCollider>().bounds.size;
            LFCGlobalManager.PlayParticle(LanternKeeper.poisonMarkObj, enemy.transform.position, enemy.transform.rotation, scaleMain: false, scaleFactor: Mathf.Max(size.x, size.y, size.z) / 2f);
            LSStatusEffectRegistry.ApplyStatus(enemy.gameObject, LSStatusEffectRegistry.StatusEffectType.POISON, playerWhoHit, 10, 100);
        }
    }
}
