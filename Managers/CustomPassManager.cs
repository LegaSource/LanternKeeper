using LanternKeeper.Behaviours;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;

namespace LanternKeeper.Managers
{
    public class CustomPassManager : MonoBehaviour
    {
        public static WallhackCustomPass wallhackPass;
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

                        wallhackPass = new WallhackCustomPass();
                        customPassVolume.customPasses.Add(wallhackPass);
                    }
                }
                return customPassVolume;
            }
        }

        public static void SetupCustomPassForGameObjects(GameObject obj)
        {
            List<Renderer> objRenderers = obj.GetComponentsInChildren<Renderer>().ToList();

            if (objRenderers == null || objRenderers.Count == 0)
            {
                LanternKeeper.mls.LogError($"No renderer could be found on {obj.name}.");
                return;
            }

            SetupCustomPass(objRenderers.ToArray());
        }

        public static void SetupCustomPass(Renderer[] renderers)
        {
            if (CustomPassVolume == null)
            {
                LanternKeeper.mls.LogError("CustomPassVolume is not assigned.");
                return;
            }

            wallhackPass = CustomPassVolume.customPasses.Find(pass => pass is WallhackCustomPass) as WallhackCustomPass;
            if (wallhackPass == null)
            {
                LanternKeeper.mls.LogError("WallhackCustomPass could not be found in CustomPassVolume.");
                return;
            }

            wallhackPass.SetTargetRenderers(renderers, LanternKeeper.wallhackShader);
        }

        public static void RemoveAura()
            => wallhackPass?.ClearTargetRenderers();
    }
}
