using GameNetcodeStuff;
using LegaFusionCore.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Behaviours;

public class Lantern : NetworkBehaviour
{
    public bool isLightOn = false;
    public bool isOutside;
    public LanternKeeperAI lanternKeeper;

    public InteractTrigger interactTrigger;
    public GameObject light1;
    public GameObject light2;

    [ClientRpc]
    public void InitializeLanternClientRpc(NetworkObjectReference enemyObject, bool isOutside)
    {
        if (enemyObject.TryGet(out NetworkObject networkObject))
            lanternKeeper = networkObject.gameObject.GetComponentInChildren<EnemyAI>() as LanternKeeperAI;

        LanternKeeper.spawnedLanterns.Add(this);

        if (isOutside)
        {
            this.isOutside = true;
            light1.GetComponent<Light>().range *= 2f;
            light2.GetComponent<Light>().range *= 2f;
        }
    }

    public void LanternInteraction() => TeleportLanternKeeperServerRpc();
    public void SwitchOnLantern() => SwitchOnLanternServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);

    [ServerRpc(RequireOwnership = false)]
    public void SwitchOnLanternServerRpc(int playerId) => SwitchOnLanternClientRpc(playerId);

    [ClientRpc]
    public void SwitchOnLanternClientRpc(int playerId)
    {
        isLightOn = true;
        interactTrigger.enabled = false;

        light1.GetComponent<Light>().enabled = true;
        light2.GetComponent<Light>().enabled = true;

        if (LFCUtilities.IsServer)
            lanternKeeper.HitEnemyOnLocalClient(20, playerWhoHit: StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>());
    }

    [ServerRpc(RequireOwnership = false)]
    public void TeleportLanternKeeperServerRpc()
    {
        if (Vector3.Distance(transform.position, lanternKeeper.transform.position) <= 15f) return;

        Vector3 position = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(transform.position);
        _ = lanternKeeper.StartCoroutine(lanternKeeper.TeleportEnemyCoroutine(position, isOutside));
        _ = lanternKeeper.SetDestinationToPosition(transform.position);
    }
}
