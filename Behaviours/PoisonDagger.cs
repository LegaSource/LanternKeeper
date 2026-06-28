using GameNetcodeStuff;
using LanternKeeper.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Behaviours;

public class PoisonDagger : PhysicsProp
{
    public AudioSource daggerAudio;
    public List<RaycastHit> objectsHitByDaggerList = [];
    public PlayerControllerB previousPlayerHeldBy;
    public RaycastHit[] objectsHitByDagger;
    public int daggerHitForce = 1;
    public AudioClip[] hitSFX;
    public AudioClip[] swingSFX;
    public int daggerMask = 1084754248;
    public float timeAtLastDamageDealt;

    public void InitializeForServer() => InitializeEveryoneRpc(UnityEngine.Random.Range(ConfigManager.daggerMinValue.Value, ConfigManager.daggerMaxValue.Value));

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void InitializeEveryoneRpc(int value) => SetScrapValue(value);//LFCUtilities.SetAddonComponent<ToxicBall>(this, Constants.TOXIC_BALL);

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        _ = RoundManager.PlayRandomClip(daggerAudio, swingSFX);
        if (playerHeldBy != null)
        {
            previousPlayerHeldBy = playerHeldBy;
            if (playerHeldBy.IsOwner) playerHeldBy.playerBodyAnimator.SetTrigger("UseHeldItem1");
        }
        if (IsOwner) HitDagger();
    }

    public void HitDagger(bool cancel = false)
    {
        if (previousPlayerHeldBy == null)
        {
            LanternKeeper.mls.LogError("Previousplayerheldby is null on this client when HitDagger is called");
            return;
        }
        previousPlayerHeldBy.activatingItem = false;
        bool hitDetected = false;
        bool hittableObjectHit = false;
        int footstepSurfaceIndex = -1;
        if (!cancel && Time.realtimeSinceStartup - timeAtLastDamageDealt > 0.43f)
        {
            previousPlayerHeldBy.twoHanded = false;
            objectsHitByDagger = Physics.SphereCastAll(previousPlayerHeldBy.gameplayCamera.transform.position + (previousPlayerHeldBy.gameplayCamera.transform.right * 0.1f), 0.5f, previousPlayerHeldBy.gameplayCamera.transform.forward, 0.75f, daggerMask, QueryTriggerInteraction.Collide);
            objectsHitByDaggerList = objectsHitByDagger.OrderBy((RaycastHit x) => x.distance).ToList();

            foreach (RaycastHit daggerHit in objectsHitByDaggerList)
            {
                if (daggerHit.transform.gameObject.layer == 8 || daggerHit.transform.gameObject.layer == 11)
                {
                    if (daggerHit.collider.isTrigger) continue;

                    hitDetected = true;
                    for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++)
                    {
                        if (StartOfRound.Instance.footstepSurfaces[i].surfaceTag == daggerHit.collider.gameObject.tag)
                        {
                            footstepSurfaceIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    if (!daggerHit.transform.TryGetComponent(out IHittable component) || daggerHit.transform == previousPlayerHeldBy.transform) continue;
                    if (!(daggerHit.point == Vector3.zero) && Physics.Linecast(previousPlayerHeldBy.gameplayCamera.transform.position, daggerHit.point, out RaycastHit _, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) continue;

                    hitDetected = true;

                    try
                    {
                        if (Time.realtimeSinceStartup - timeAtLastDamageDealt > 0.43f)
                        {
                            timeAtLastDamageDealt = Time.realtimeSinceStartup;
                            _ = component.Hit(daggerHitForce, previousPlayerHeldBy.gameplayCamera.transform.forward, previousPlayerHeldBy, playHitSFX: true, 5);
                        }
                        hittableObjectHit = true;
                    }
                    catch (Exception arg)
                    {
                        LanternKeeper.mls.LogError($"Exception caught when hitting object with dagger from player #{previousPlayerHeldBy.playerClientId}: {arg}");
                    }
                }
            }
        }
        if (hitDetected)
        {
            _ = RoundManager.PlayRandomClip(daggerAudio, hitSFX);
            FindObjectOfType<RoundManager>().PlayAudibleNoise(transform.position, 17f, 0.8f);
            if (!hittableObjectHit && footstepSurfaceIndex != -1)
            {
                daggerAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[footstepSurfaceIndex].hitSurfaceSFX);
                WalkieTalkie.TransmitOneShotAudio(daggerAudio, StartOfRound.Instance.footstepSurfaces[footstepSurfaceIndex].hitSurfaceSFX);
            }
            HitDaggerEveryoneRpc(footstepSurfaceIndex);
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void HitDaggerEveryoneRpc(int hitSurfaceID)
    {
        if (IsOwner) return;

        _ = RoundManager.PlayRandomClip(daggerAudio, hitSFX);
        if (hitSurfaceID != -1) HitSurfaceWithDagger(hitSurfaceID);
    }

    public void HitSurfaceWithDagger(int hitSurfaceID)
    {
        daggerAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
        WalkieTalkie.TransmitOneShotAudio(daggerAudio, StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
    }
}
