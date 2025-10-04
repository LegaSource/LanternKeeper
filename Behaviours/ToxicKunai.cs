using GameNetcodeStuff;
using LanternKeeper.Managers;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Behaviours;

public class ToxicKunai : NetworkBehaviour
{
    public Rigidbody rigidbody;
    public PlayerControllerB throwingPlayer;
    public AudioSource kunaiAudio;
    public AudioClip[] throwSFX;

    [ClientRpc]
    public void ShootToxicKunaiClientRpc(int playerId)
    {
        throwingPlayer = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        _ = RoundManager.PlayRandomClip(kunaiAudio, throwSFX);

        Vector3 throwDirection = throwingPlayer.gameplayCamera.transform.forward;
        float minY = -2f;
        if (throwDirection.y < minY) throwDirection = new Vector3(throwDirection.x, minY, throwDirection.z).normalized;
        Vector3 horizontalVelocity = throwDirection * 60f; // Vitesse horizontale
        Vector3 verticalVelocity = new Vector3(0, 3f, 0); // Vitesse verticale pour créer l'arc

        // Réinitialisation de la vélocité avant d'appliquer la nouvelle force
        rigidbody.velocity = Vector3.zero;
        rigidbody.AddForce(horizontalVelocity, ForceMode.VelocityChange);
        rigidbody.AddForce(verticalVelocity, ForceMode.VelocityChange);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || throwingPlayer == null) return;
        if (GameNetworkManager.Instance.localPlayerController != throwingPlayer) return;

        if (HandleEnemyHitFromPlayer(other, throwingPlayer)
            || HandlePlayerHitFromPlayer(other, throwingPlayer))
        {
            DestroyToxicKunaiServerRpc();
        }
    }

    public bool HandleEnemyHitFromPlayer(Collider other, PlayerControllerB throwingPlayer)
    {
        EnemyAICollisionDetect enemyCollision = other.GetComponent<EnemyAICollisionDetect>();
        if (enemyCollision == null) return false;

        LanternKeeperNetworkManager.Instance.SpawnToxicFangServerRpc((int)throwingPlayer.playerClientId, enemyCollision.mainScript.NetworkObject);
        return true;
    }

    public bool HandlePlayerHitFromPlayer(Collider other, PlayerControllerB throwingPlayer)
    {
        PlayerControllerB player = other.GetComponent<PlayerControllerB>();
        if (player == null || player == throwingPlayer) return false;

        LanternKeeperNetworkManager.Instance.SpawnToxicFangServerRpc((int)throwingPlayer.playerClientId, (int)player.playerClientId);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyToxicKunaiServerRpc() => Destroy(gameObject);
}
