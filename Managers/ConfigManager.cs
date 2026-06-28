using BepInEx.Configuration;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LanternKeeper.Managers;

public class ConfigManager
{
    // GLOBAL
    public static ConfigEntry<string> spawnWeights;
    // LANTERN KEEPER
    public static ConfigEntry<int> enemyDirectDamage;
    public static ConfigEntry<int> enemyPoisonDamage;
    public static ConfigEntry<int> enemyPoisonDuration;
    // POISON DAGGER
    public static ConfigEntry<int> daggerMinValue;
    public static ConfigEntry<int> daggerMaxValue;
    // POISON MARK
    public static ConfigEntry<int> poisonMarkCooldown;

    public static void Load()
    {
        // GLOBAL
        spawnWeights = LanternKeeper.configFile.Bind(Constants.GLOBAL, "Spawn weights", "Vanilla:20,Modded:20", $"{Constants.LANTERN_KEEPER_ENEMY} spawn weights");
        // LANTERN KEEPER
        enemyDirectDamage = LanternKeeper.configFile.Bind(Constants.LANTERN_KEEPER_ENEMY, "Direct Damage", 30, $"Direct damage dealt by the {Constants.LANTERN_KEEPER}");
        enemyPoisonDamage = LanternKeeper.configFile.Bind(Constants.LANTERN_KEEPER_ENEMY, "Poison Damage", 10, "Total damage dealt by the poison to the player");
        enemyPoisonDuration = LanternKeeper.configFile.Bind(Constants.LANTERN_KEEPER_ENEMY, "Poison Duration", 10, "Poison duration");
        // POISON DAGGER
        daggerMinValue = LanternKeeper.configFile.Bind(Constants.POISON_DAGGER, "Min Value", 50, $"{Constants.POISON_DAGGER} min value");
        daggerMaxValue = LanternKeeper.configFile.Bind(Constants.POISON_DAGGER, "Max Value", 90, $"{Constants.POISON_DAGGER} max value");
        // TOXIC FANG
        poisonMarkCooldown = LanternKeeper.configFile.Bind(Constants.POISON_MARK, "Cooldown", 45, $"Cooldown duration of the {Constants.POISON_MARK}");
    }

    public static (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) GetEnemySpawns()
    {
        Dictionary<Levels.LevelTypes, int> spawnRateByLevelType = [];
        Dictionary<string, int> spawnRateByCustomLevelType = [];
        foreach (string spawnWeight in spawnWeights.Value.Split(',').Select(s => s.Trim()))
        {
            string[] values = spawnWeight.Split(':');
            if (values.Length != 2) continue;

            string name = values[0];
            if (int.TryParse(values[1], out int spawnRate))
            {
                if (Enum.TryParse(name, ignoreCase: true, out Levels.LevelTypes levelType)) spawnRateByLevelType[levelType] = spawnRate;
                else spawnRateByCustomLevelType[name] = spawnRate;
            }
        }
        return (spawnRateByLevelType, spawnRateByCustomLevelType);
    }
}
