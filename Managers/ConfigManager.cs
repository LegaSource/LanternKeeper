using BepInEx.Configuration;

namespace LanternKeeper.Managers
{
    public class ConfigManager
    {
        // GLOBAL
        public static ConfigEntry<int> rarity;
        // LANTERN KEEPER
        public static ConfigEntry<float> angerIncrement;
        public static ConfigEntry<float> angerIncrementLast;
        public static ConfigEntry<int> enemyPoisonDamage;
        public static ConfigEntry<int> enemyPoisonDuration;
        public static ConfigEntry<float> enemyPoisonIntensity;
        // POISON DAGGER
        public static ConfigEntry<int> daggerPoisonDamage;
        public static ConfigEntry<int> daggerPoisonDuration;
        public static ConfigEntry<float> daggerPoisonStunDuration;
        // FORTUNE COOKIE
        public static ConfigEntry<float> auraDuration;


        public static void Load()
        {
            // GLOBAL
            rarity = LanternKeeper.configFile.Bind(Constants.GLOBAL, "Rarity", 50, $"{Constants.LANTERN_KEEPER} rarity");
            // LANTERN KEEPER
            angerIncrement = LanternKeeper.configFile.Bind(Constants.LANTERN_KEEPER_ENEMY, "Anger Increment", 0.2f, "Anger increment - this value starts at 1 and is used as a multiplier for the enemy's speed and the (direct) damage they inflict");
            angerIncrementLast = LanternKeeper.configFile.Bind(Constants.LANTERN_KEEPER_ENEMY, "Anger Increment Last", 0.4f, "Anger increment when the last lantern is lit");
            enemyPoisonDamage = LanternKeeper.configFile.Bind(Constants.LANTERN_KEEPER_ENEMY, "Poison Damage", 1, "Damage dealt by the poison to the player every second");
            enemyPoisonDuration = LanternKeeper.configFile.Bind(Constants.LANTERN_KEEPER_ENEMY, "Poison Duration", 10, "Poison duration");
            enemyPoisonIntensity = LanternKeeper.configFile.Bind(Constants.LANTERN_KEEPER_ENEMY, "Poison Intensity", 0.2f, "Intensity of the poison filter");
            // POISON DAGGER
            daggerPoisonDamage = LanternKeeper.configFile.Bind(Constants.POISON_DAGGER, "Poison Damage", 1, "Damage dealt to the enemy at the end of the poisoning (when the 'poison duration' is over)");
            daggerPoisonDuration = LanternKeeper.configFile.Bind(Constants.POISON_DAGGER, "Poison Duration", 10, "Poison duration");
            daggerPoisonStunDuration = LanternKeeper.configFile.Bind(Constants.POISON_DAGGER, "Poison Stun Duration", 0.02f, "Stun duration applied to the enemy for each second of poisoning");
            // FORTUNE COOKIE
            auraDuration = LanternKeeper.configFile.Bind(Constants.POISON_DAGGER, "Aura Duration", 30f, "Duration of aura to see the lanterns");
        }
    }
}
