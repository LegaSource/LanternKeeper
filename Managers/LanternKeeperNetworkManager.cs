using GameNetcodeStuff;
using LanternKeeper.Behaviours;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Managers;

internal class LanternKeeperNetworkManager : NetworkBehaviour
{
    public static LanternKeeperNetworkManager Instance;

    public void Awake() => Instance = this;

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void ThrowPoisonBallServerRpc(int playerId, Vector3 position, Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.5f)
            direction = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>().transform.forward;
        direction = direction.normalized;

        GameObject gameObject = Instantiate(LanternKeeper.poisonBallObj, position, Quaternion.identity);
        gameObject.GetComponent<NetworkObject>().Spawn();
        gameObject.GetComponent<PoisonBall>().ThrowFromPlayerEveryoneRpc(playerId, position, direction);
    }
}
