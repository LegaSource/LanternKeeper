using AddonFusion;
using HarmonyLib;
using LanternKeeper.Behaviours;
using LanternKeeperAddon.Behaviours.AddonComponents;

namespace LanternKeeperAddon.Patches;

public class PoisonDaggerPatch
{
    [HarmonyPatch(typeof(PoisonDagger), nameof(PoisonDagger.InitializeEveryoneRpc))]
    [HarmonyPostfix]
    public static void InitializeForEveryone(PoisonDagger __instance) => AFUtilities.SetAddonComponent<PoisonMark>(__instance);
}
