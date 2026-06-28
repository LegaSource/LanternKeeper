using AddonFusion.Behaviours.AddonComponents;
using AddonFusion.Behaviours.Scripts;
using GameNetcodeStuff;
using LanternKeeper;
using LanternKeeper.Managers;
using LegaFusionCore.Utilities;
using UnityEngine;
using static AddonFusion.Behaviours.Scripts.AddonTargetDatabase;

namespace LanternKeeperAddon.Behaviours.AddonComponents;

[AddonInfo(AddonTargetType.ALL)]
public class PoisonMark : AddonComponent
{
    public override string AddonName => Constants.POISON_MARK;
    public override bool IsPassive => false;

    private readonly Collider[] overlapBuffer = new Collider[64];
    public readonly float AoERadius = 1f;
    public readonly int AoEMask = 1084754248;

    public AoEProjector poisonProjector;

    public override void ActivateAddonAbility()
    {
        if (!onCooldown && StartOfRound.Instance.shipHasLanded && grabbableObject.playerHeldBy != null)
        {
            isEnabled = !isEnabled;
            if (isEnabled)
            {
                GameObject projectorObj = Instantiate(LanternKeeperAddon.poisonProjectorObj, grabbableObject.playerHeldBy.transform.position, Quaternion.identity);
                poisonProjector = projectorObj.GetComponent<AoEProjector>();
            }
            else if (poisonProjector != null)
            {
                if (poisonProjector.TryConfirm(out Vector3 position))
                {
                    int count = Physics.OverlapSphereNonAlloc(position, AoERadius, overlapBuffer, AoEMask, QueryTriggerInteraction.Collide);
                    for (int i = 0; i < count; i++)
                    {
                        Collider collider = overlapBuffer[i];
                        if (collider != null)
                        {
                            if (collider.gameObject.TryGetComponent(out PlayerControllerB player) && !player.isPlayerDead && LFCUtilities.ShouldNotBeLocalPlayer(player))
                                LanternKeeperNetworkManager.Instance.SpawnPoisonMarkEveryoneRpc((int)player.playerClientId, (int)LFCUtilities.LocalPlayer.playerClientId);
                            if (collider.gameObject.TryGetComponent(out EnemyAICollisionDetect collision) && collision.mainScript != null && !collision.mainScript.isEnemyDead)
                                LanternKeeperNetworkManager.Instance.SpawnPoisonMarkEveryoneRpc(collision.mainScript.NetworkObject, (int)LFCUtilities.LocalPlayer.playerClientId);
                        }
                    }
                }
                Destroy(poisonProjector.gameObject);
                StartCooldown(ConfigManager.poisonMarkCooldown.Value);
            }
        }
    }

    public void Update()
    {
        if (isEnabled && (grabbableObject == null || !grabbableObject.isHeld || grabbableObject.isPocketed) && poisonProjector != null)
        {
            isEnabled = false;
            Destroy(poisonProjector.gameObject);
        }
    }
}
