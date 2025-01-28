using GameNetcodeStuff;
using LanternKeeper.Managers;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Behaviours
{
    public class FortuneCookie : PhysicsProp
    {
        public static Coroutine showLanternCoroutine;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown && playerHeldBy != null)
            {
                if (!LanternKeeper.spawnedLanterns.Any()) return;

                Lantern currentLantern = LanternKeeper.spawnedLanterns[LanternKeeper.currentLanternToLightIndex];
                if (currentLantern == null) return;

                int randomHelp = new System.Random().Next(0, 3);
                switch (randomHelp)
                {
                    case 0:
                        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts
                            .Where(p => p.isPlayerControlled && !p.isPlayerDead)
                            .OrderBy(p => Vector3.Distance(p.transform.position, currentLantern.transform.position))
                            .FirstOrDefault();
                        HUDManager.Instance.DisplayTip(Constants.INFORMATION, player.playerUsername + Constants.MESSAGE_INFO_LANTERN_HELP1 + Vector3.Distance(player.transform.position, currentLantern.transform.position));
                        break;
                    case 1:
                        if (showLanternCoroutine != null)
                            StopCoroutine(showLanternCoroutine);
                        showLanternCoroutine = StartCoroutine(ShowLanternCoroutine(currentLantern));
                        HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_LANTERN_HELP2);
                        break;
                    case 2:
                        HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_LANTERN_HELP3 + LKUtilities.GetLanternColor(currentLantern.currentColorIndex));
                        break;
                }
                DestroyObjectServerRpc();
            }
        }

        public static IEnumerator ShowLanternCoroutine(Lantern currentLantern)
        {
            CustomPassManager.RemoveAura();
            CustomPassManager.SetupCustomPassForGameObjects(currentLantern.gameObject);

            yield return new WaitForSeconds(ConfigManager.auraDuration.Value);

            CustomPassManager.RemoveAura();
            showLanternCoroutine = null;
        }

        [ServerRpc(RequireOwnership = false)]
        public void DestroyObjectServerRpc()
            => DestroyObjectClientRpc();

        [ClientRpc]
        public void DestroyObjectClientRpc()
            => DestroyObjectInHand(playerHeldBy);
    }
}
