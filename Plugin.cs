using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LanternKeeper.Behaviours;
using LanternKeeper.Managers;
using LanternKeeper.Patches;
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
    private const string modGUID = "Lega.LanternKeeper";
    private const string modName = "Lantern Keeper";
    private const string modVersion = "1.0.4";

    private readonly Harmony harmony = new Harmony(modGUID);
    private static readonly AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lanternkeeper"));
    internal static ManualLogSource mls;
    public static ConfigFile configFile;

    // Enemies
    public static EnemyType lanternKeeperEnemy;

    // Items
    public static GameObject fortuneCookieObj;
    public static GameObject daggerObj;

    // Lanterns
    public static GameObject lanternObj;
    public static List<Lantern> spawnedLanterns = [];

    // Particles
    public static GameObject poisonParticle;

    // Shaders
    public static Material wallhackShader;

    // Lights
    public static GameObject redLight;
    public static GameObject blueLight;
    public static GameObject greenLight;

    public static bool teleportOnCooldown = false;
    public enum ControlTip
    {
        RED,
        BLUE,
        GREEN
    }

    public static List<int> shuffledLights =
    [
        (int)ControlTip.RED,
        (int)ControlTip.BLUE,
        (int)ControlTip.GREEN
    ];
    public static int currentLanternToLightIndex;

    public void Awake()
    {
        mls = BepInEx.Logging.Logger.CreateLogSource("LanternKeeper");
        configFile = Config;
        ConfigManager.Load();

        NetcodePatcher();
        LoadItems();
        LoadLantern();
        LoadLights();
        LoadEnemies();
        LoadParticles();
        LoadShaders();

        harmony.PatchAll(typeof(RoundManagerPatch));
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

    public void LoadItems()
    {
        fortuneCookieObj = RegisterItem(typeof(FortuneCookie), bundle.LoadAsset<Item>("Assets/FortuneCookie/FortuneCookieItem.asset")).spawnPrefab;
        daggerObj = RegisterItem(typeof(PoisonDagger), bundle.LoadAsset<Item>("Assets/PoisonDagger/PoisonDaggerItem.asset")).spawnPrefab;
    }

    public Item RegisterItem(Type type, Item item)
    {
        if (item.spawnPrefab.GetComponent<PhysicsProp>() == null)
        {
            PhysicsProp script = item.spawnPrefab.AddComponent(type) as PhysicsProp;
            script.grabbable = true;
            script.grabbableToEnemies = true;
            script.itemProperties = item;
        }

        NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
        Utilities.FixMixerGroups(item.spawnPrefab);
        Items.RegisterItem(item);

        return item;
    }

    public void LoadLantern()
    {
        lanternObj = bundle.LoadAsset<GameObject>("Assets/Lantern/LK_Lantern.prefab");
        NetworkPrefabs.RegisterNetworkPrefab(lanternObj);
        Utilities.FixMixerGroups(lanternObj);
    }

    public static void LoadLights()
    {
        redLight = bundle.LoadAsset<GameObject>("Assets/Lantern/RedLight.prefab");
        blueLight = bundle.LoadAsset<GameObject>("Assets/Lantern/BlueLight.prefab");
        greenLight = bundle.LoadAsset<GameObject>("Assets/Lantern/GreenLight.prefab");
    }

    public static void LoadEnemies()
    {
        lanternKeeperEnemy = bundle.LoadAsset<EnemyType>("Assets/LanternKeeper/LanternKeeperEnemy.asset");
        NetworkPrefabs.RegisterNetworkPrefab(lanternKeeperEnemy.enemyPrefab);
        Enemies.RegisterEnemy(lanternKeeperEnemy, 0, Levels.LevelTypes.None, bundle.LoadAsset<TerminalNode>("Assets/LanternKeeper/LanternKeeperTN.asset"), bundle.LoadAsset<TerminalKeyword>("Assets/LanternKeeper/LanternKeeperTK.asset"));
    }

    public void LoadParticles()
    {
        HashSet<GameObject> gameObjects =
        [
            (poisonParticle = bundle.LoadAsset<GameObject>("Assets/Particles/PoisonParticle.prefab"))
        ];

        foreach (GameObject gameObject in gameObjects)
        {
            NetworkPrefabs.RegisterNetworkPrefab(gameObject);
            Utilities.FixMixerGroups(gameObject);
        }
    }

    public static void LoadShaders()
        => wallhackShader = bundle.LoadAsset<Material>("Assets/Shaders/WallhackMaterial.mat");
}
