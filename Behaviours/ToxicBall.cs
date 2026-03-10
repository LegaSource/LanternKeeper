using GameNetcodeStuff;
using LanternKeeper.Managers;
using LegaFusionCore.Behaviours.Addons;
using UnityEngine;

namespace LanternKeeper.Behaviours;

public class ToxicBall : AddonComponent
{
    public override void ActivateAddonAbility()
    {
        if (onCooldown || !StartOfRound.Instance.shipHasLanded) return;

        PlayerControllerB player = GetComponentInParent<GrabbableObject>()?.playerHeldBy;
        if (player != null)
        {
            Vector3 position = player.localVisor.transform.position + player.gameplayCamera.transform.forward;
            StartCooldown(ConfigManager.toxicBallCooldown.Value);
            LanternKeeperNetworkManager.Instance.ThrowPoisonBallServerRpc((int)player.playerClientId, position, player.gameplayCamera.transform.forward);
        }
    }
}