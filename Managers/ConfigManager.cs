using BepInEx.Configuration;

namespace LanternKeeper.Managers;

public class ConfigManager
{
    // GLOBAL
    public static ConfigEntry<int> rarity;
    // LANTERN
    public static ConfigEntry<float> teleportationCooldown;
    // LANTERN KEEPER
    public static ConfigEntry<float> angerIncrement;
    public static ConfigEntry<int> enemyPoisonDamage;
    public static ConfigEntry<int> enemyPoisonDuration;
    public static ConfigEntry<float> enemyPoisonIntensity;
    // POISON DAGGER
    public static ConfigEntry<int> daggerMinValue;
    public static ConfigEntry<int> daggerMaxValue;
    public static ConfigEntry<int> daggerPoisonDamage;
    public static ConfigEntry<int> daggerPoisonDuration;
    public static ConfigEntry<float> daggerPoisonStunDuration;
    // FORTUNE COOKIE
    public static ConfigEntry<float> auraDuration;

    public static void Load()
    {
        // GLOBAL
        rarity = LanternKeeper.configFile.Bind(Constants.GLOBAL, "Rarity", 20, $"{Constants.LANTERN_KEEPER} rarity");
        // LANTERN
        teleportationCooldown = LanternKeeper.configFile.Bind(Constants.LANTERN, "Teleport Cooldown", 30f, "Cooldown for teleportation between lanterns");
        // LANTERN KEEPER
        angerIncrement = LanternKeeper.configFile.Bind(Constants.LANTERN_KEEPER_ENEMY, "Anger Increment", 0.4f, "Anger increment - this value starts at 1 and is used as a multiplier for the enemy's speed and the (direct) damage they inflict");
        enemyPoisonDamage = LanternKeeper.configFile.Bind(Constants.LANTERN_KEEPER_ENEMY, "Poison Damage", 1, "Damage dealt by the poison to the player every second");
        enemyPoisonDuration = LanternKeeper.configFile.Bind(Constants.LANTERN_KEEPER_ENEMY, "Poison Duration", 10, "Poison duration");
        enemyPoisonIntensity = LanternKeeper.configFile.Bind(Constants.LANTERN_KEEPER_ENEMY, "Poison Intensity", 0.2f, "Intensity of the poison filter");
        // POISON DAGGER
        daggerMinValue = LanternKeeper.configFile.Bind(Constants.POISON_DAGGER, "Min Value", 50, $"{Constants.POISON_DAGGER} min value");
        daggerMaxValue = LanternKeeper.configFile.Bind(Constants.POISON_DAGGER, "Max Value", 90, $"{Constants.POISON_DAGGER} max value");
        daggerPoisonDamage = LanternKeeper.configFile.Bind(Constants.POISON_DAGGER, "Poison Damage", 1, "Damage dealt to the enemy at the end of the poisoning (when the 'poison duration' is over)");
        daggerPoisonDuration = LanternKeeper.configFile.Bind(Constants.POISON_DAGGER, "Poison Duration", 5, "Poison duration");
        daggerPoisonStunDuration = LanternKeeper.configFile.Bind(Constants.POISON_DAGGER, "Poison Stun Duration", 0.1f, "Stun duration applied to the enemy for each second of poisoning");
        // FORTUNE COOKIE
        auraDuration = LanternKeeper.configFile.Bind(Constants.POISON_DAGGER, "Aura Duration", 10f, "Duration of aura to see the lanterns");
    }
}
