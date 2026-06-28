using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LanternKeeper.Behaviours;
using LanternKeeper.Managers;
using LanternKeeper.Patches;
using LegaFusionCore.Managers;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LanternKeeper;

[BepInPlugin(modGUID, modName, modVersion)]
public class LanternKeeper : BaseUnityPlugin
{
    internal const string modGUID = "Lega.LanternKeeper";
    internal const string modName = "Lantern Keeper";
    internal const string modVersion = "1.0.9";

    private readonly Harmony harmony = new Harmony(modGUID);
    private static readonly AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lanternkeeper"));
    internal static ManualLogSource mls;
    public static ConfigFile configFile;

    public static GameObject managerPrefab = NetworkPrefabs.CreateNetworkPrefab("LanternKeeperNetworkManager");

    public static GameObject daggerObj;
    public static GameObject poisonMarkObj;
    public static GameObject poisonBallObj;
    public static GameObject lanternObj;
    public static List<Lantern> spawnedLanterns = [];

    public void Awake()
    {
        mls = BepInEx.Logging.Logger.CreateLogSource("LanternKeeper");
        configFile = Config;
        ConfigManager.Load();

        LoadManager();
        NetcodePatcher();
        LoadItems();
        LoadEnemies();
        LoadNetworkPrefabs();

        harmony.PatchAll(typeof(StartOfRoundPatch));
    }

    public static void LoadManager()
    {
        Utilities.FixMixerGroups(managerPrefab);
        _ = managerPrefab.AddComponent<LanternKeeperNetworkManager>();
    }

    private static void NetcodePatcher()
    {
        Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (Type type in types)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                object[] attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length == 0) continue;
                _ = method.Invoke(null, null);
            }
        }
    }

    public void LoadItems() => daggerObj = LFCObjectsManager.RegisterObject(typeof(PoisonDagger), bundle.LoadAsset<Item>("Assets/PoisonDagger/PoisonDaggerItem.asset")).spawnPrefab;

    public static void LoadEnemies()
    {
        EnemyType lanternKeeperEnemy = bundle.LoadAsset<EnemyType>("Assets/LanternKeeper/LanternKeeperEnemy.asset");
        NetworkPrefabs.RegisterNetworkPrefab(lanternKeeperEnemy.enemyPrefab);

        (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigManager.GetEnemySpawns();
        Enemies.RegisterEnemy(lanternKeeperEnemy,
            spawnRateByLevelType,
            spawnRateByCustomLevelType,
            bundle.LoadAsset<TerminalNode>("Assets/LanternKeeper/LanternKeeperTN.asset"),
            bundle.LoadAsset<TerminalKeyword>("Assets/LanternKeeper/LanternKeeperTK.asset"));
    }

    public void LoadNetworkPrefabs()
    {
        HashSet<GameObject> gameObjects =
        [
            (lanternObj = bundle.LoadAsset<GameObject>("Assets/Lantern/LK_Lantern.prefab")),
            (poisonBallObj = bundle.LoadAsset<GameObject>("Assets/PoisonBall/PoisonBall.prefab")),
            (poisonMarkObj = bundle.LoadAsset<GameObject>("Assets/Addons/PoisonMark.prefab"))
        ];

        foreach (GameObject gameObject in gameObjects)
        {
            NetworkPrefabs.RegisterNetworkPrefab(gameObject);
            Utilities.FixMixerGroups(gameObject);
        }
    }
}
