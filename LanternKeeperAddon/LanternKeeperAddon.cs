using AddonFusion.Registries;
using BepInEx;
using HarmonyLib;
using LanternKeeper;
using LanternKeeperAddon.Behaviours.AddonComponents;
using LanternKeeperAddon.Behaviours.AddonProps;
using LanternKeeperAddon.Patches;
using LegaFusionCore.Managers;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LanternKeeperAddon;

[BepInPlugin(modGUID, modName, modVersion)]
[BepInDependency("Lega.LanternKeeper", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("Lega.AddonFusion", BepInDependency.DependencyFlags.HardDependency)]
public class LanternKeeperAddon : BaseUnityPlugin
{
    public const string modGUID = "Lega.LanternKeeperAddon";
    public const string modName = "Lantern Keeper Addon";
    public const string modVersion = "1.0.0";

    private readonly Harmony harmony = new Harmony(modGUID);
    private static readonly AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lanternkeeperaddon"));

    public static GameObject poisonProjectorObj;

    public void Awake()
    {
        LoadItems();
        LoadPrefabs();
        harmony.PatchAll(typeof(PoisonDaggerPatch));
    }

    public void LoadItems() => RegisterAddon(typeof(PoisonMark), Constants.POISON_MARK, typeof(PoisonMarkItem), bundle.LoadAsset<Item>("Assets/AddonProps/PoisonMarkItem.asset"));

    public void RegisterAddon(Type addonType, string addonName, Type itemType, Item item)
    {
        item = LFCObjectsManager.RegisterObject(itemType, item);
        AddonObjectRegistry.Add(addonType, addonName, item.spawnPrefab);
    }

    public void LoadPrefabs()
    {
        HashSet<GameObject> gameObjects =
        [
            (poisonProjectorObj = bundle.LoadAsset<GameObject>("Assets/AoEProjector/PoisonProjector.prefab"))
        ];

        foreach (GameObject gameObject in gameObjects)
            Utilities.FixMixerGroups(gameObject);
    }
}