using GameNetcodeStuff;
using LanternKeeper.Managers;
using LegaFusionCore.Behaviours.Addons;

namespace LanternKeeper.Behaviours;

public class ToxicFang : AddonComponent
{
    public override void ActivateAddonAbility()
    {
        if (onCooldown || !StartOfRound.Instance.shipHasLanded) return;

        PlayerControllerB player = GetComponentInParent<GrabbableObject>()?.playerHeldBy;
        if (player == null) return;

        StartCooldown(ConfigManager.toxicFangCooldown.Value);
        LanternKeeperNetworkManager.Instance.ShootToxicKunaiServerRpc((int)player.playerClientId);
    }
}