using HarmonyLib;
using LanternKeeper.Behaviours;
using LanternKeeper.Managers;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper.Patches;

internal class StartOfRoundPatch
{
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    [HarmonyBefore(["evaisa.lethallib"])]
    [HarmonyPostfix]
    private static void StartRound(ref StartOfRound __instance)
    {
        if (NetworkManager.Singleton.IsHost && LanternKeeperNetworkManager.Instance == null)
        {
            GameObject gameObject = Object.Instantiate(LanternKeeper.managerPrefab, __instance.transform.parent);
            gameObject.GetComponent<NetworkObject>().Spawn();
            LanternKeeper.mls.LogInfo("Spawning LanternKeeperNetworkManager");
        }
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame))]
    [HarmonyPostfix]
    public static void EndOfGame()
    {
        foreach (Lantern lantern in LanternKeeper.spawnedLanterns)
        {
            if (lantern == null || !lantern.IsSpawned) continue;
            lantern.GetComponent<NetworkObject>().Despawn(destroy: true);
        }
        LanternKeeper.spawnedLanterns.Clear();
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnDisable))]
    [HarmonyPostfix]
    public static void OnDisable() => LanternKeeperNetworkManager.Instance = null;
}
