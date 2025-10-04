using GameNetcodeStuff;
using LanternKeeper.Behaviours;
using LegaFusionCore.Registries;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Managers;

internal class LanternKeeperNetworkManager : NetworkBehaviour
{
    public static LanternKeeperNetworkManager Instance;

    public void Awake() => Instance = this;

    [ServerRpc(RequireOwnership = false)]
    public void ShootToxicKunaiServerRpc(int playerId)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        Vector3 position = player.currentlyHeldObjectServer.transform.position - (player.currentlyHeldObjectServer.transform.forward * 0.5f);

        GameObject gameObject = Instantiate(LanternKeeper.toxicKunaiObj, position, player.gameplayCamera.transform.rotation, StartOfRound.Instance.propsContainer);
        ToxicKunai toxicKunai = gameObject.GetComponent<ToxicKunai>();
        gameObject.GetComponent<NetworkObject>().Spawn();
        toxicKunai.ShootToxicKunaiClientRpc(playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnToxicFangServerRpc(int playerId, NetworkObjectReference enemyObj) => ApplyPoisonClientRpc(playerId, enemyObj);

    [ClientRpc]
    public void ApplyPoisonClientRpc(int playerId, NetworkObjectReference enemyObj)
    {
        if (!enemyObj.TryGet(out NetworkObject networkObjectEnemy)) return;

        EnemyAI enemy = networkObjectEnemy.gameObject.GetComponentInChildren<EnemyAI>();
        LFCStatusEffectRegistry.ApplyStatus(enemy.gameObject, LFCStatusEffectRegistry.StatusEffectType.POISON, playerId, 10, 100);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnToxicFangServerRpc(int playerId, int targetId) => ApplyPoisonClientRpc(playerId, targetId);

    [ClientRpc]
    public void ApplyPoisonClientRpc(int playerId, int targetId)
    {
        PlayerControllerB targetedPlayer = StartOfRound.Instance.allPlayerObjects[targetId].GetComponent<PlayerControllerB>();
        LFCStatusEffectRegistry.ApplyStatus(targetedPlayer.gameObject, LFCStatusEffectRegistry.StatusEffectType.POISON, playerId, 10, 10);
    }
}
