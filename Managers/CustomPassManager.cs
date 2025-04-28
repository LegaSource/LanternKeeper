using LanternKeeper.Behaviours;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace LanternKeeper.Managers;

public class CustomPassManager : MonoBehaviour
{
    public static AuraCustomPass auraPass;
    public static CustomPassVolume customPassVolume;

    public static CustomPassVolume CustomPassVolume
    {
        get
        {
            if (customPassVolume == null)
            {
                customPassVolume = GameNetworkManager.Instance.localPlayerController.gameplayCamera.gameObject.AddComponent<CustomPassVolume>();
                if (customPassVolume != null)
                {
                    customPassVolume.targetCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
                    customPassVolume.injectionPoint = (CustomPassInjectionPoint)1;
                    customPassVolume.isGlobal = true;

                    auraPass = new AuraCustomPass();
                    customPassVolume.customPasses.Add(auraPass);
                }
            }
            return customPassVolume;
        }
    }

    public static void SetupAuraForObjects(GameObject[] objects, Material material)
    {
        Renderer[] renderers = GetFilteredRenderersFromObjects(objects);
        if (renderers.Length > 0) SetupCustomPass(renderers, material);
    }

    public static void SetupCustomPass(Renderer[] renderers, Material material)
    {
        if (CustomPassVolume == null)
        {
            LanternKeeper.mls.LogError("CustomPassVolume is not assigned.");
            return;
        }

        auraPass = CustomPassVolume.customPasses.Find(pass => pass is AuraCustomPass) as AuraCustomPass;
        if (auraPass == null)
        {
            LanternKeeper.mls.LogError("AuraCustomPass could not be found in CustomPassVolume.");
            return;
        }

        auraPass.AddTargetRenderers(renderers, material);
    }

    private static Renderer[] GetFilteredRenderersFromObjects(GameObject[] objects)
    {
        List<Renderer> collectedRenderers = [];

        foreach (GameObject obj in objects)
        {
            if (obj == null) continue;

            List<Renderer> renderers = obj.GetComponentsInChildren<Renderer>().ToList();
            if (renderers.Count == 0)
            {
                LanternKeeper.mls.LogError($"No renderer could be found on {obj.name}.");
                continue;
            }

            collectedRenderers.AddRange(renderers);
        }

        return collectedRenderers.ToArray();
    }

    public static void ClearAura()
        => auraPass?.ClearTargetRenderers();
}
