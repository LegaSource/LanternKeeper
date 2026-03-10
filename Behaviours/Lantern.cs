using GameNetcodeStuff;
using LegaFusionCore.Managers.NetworkManagers;
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

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void InitializeLanternEveryoneRpc(NetworkObjectReference enemyObject, bool isOutside)
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
    public void SwitchOnLantern() => SwitchOnLanternEveryoneRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SwitchOnLanternEveryoneRpc(int playerId)
    {
        isLightOn = true;
        interactTrigger.enabled = false;

        light1.GetComponent<Light>().enabled = true;
        light2.GetComponent<Light>().enabled = true;

        if (LFCUtilities.IsServer)
            lanternKeeper.HitEnemyOnLocalClient(20, playerWhoHit: StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>());
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void TeleportLanternKeeperServerRpc()
    {
        if (Vector3.Distance(transform.position, lanternKeeper.transform.position) > 15f)
        {
            Vector3 position = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(transform.position);
            LFCNetworkManager.Instance.TeleportEnemyEveryoneRpc(lanternKeeper.thisNetworkObject, position, isOutside);
            _ = lanternKeeper.SetDestinationToPosition(transform.position);
        }
    }
}
