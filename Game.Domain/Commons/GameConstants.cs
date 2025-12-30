namespace Game.Domain.Commons;

public static class GameConstants
{
    public static class World
    {
        public const int TICKS_PER_SECOND = 60;
    }

    /// <summary>
    /// Escalas fixas (inteiros) para evitar float/double e manter determinismo.
    /// </summary>
    public static class Scaling
    {
        /// <summary>
        /// GrowthModifiers: 10 = 1.0 por level (ex.: 13 => 1.3 por level).
        /// </summary>
        public const int GROWTH_SCALE = 10;

        /// <summary>
        /// Permille (‰): 1000 = 1.000x (multiplicador). Ex.: 1500 = 1.5x.
        /// </summary>
        public const int MULTIPLIER_PERMILLE = 1000;

        /// <summary>
        /// Basis points (BPS): 10_000 = 100.00%.
        /// </summary>
        public const int BPS_SCALE = 10_000;
    }

    public static class Character
    {
        public const int BASE_LEVEL = 1;
        public const int MAX_LEVEL = 1000;
        public const int BASE_HEALTH = 100;
        public const int BASE_MANA = 50;
    }

    public static class Combat
    {
        // Attack speed (multiplicador em permille)
        public const int ATTACK_SPEED_SCALE = Scaling.MULTIPLIER_PERMILLE; // 1000 = 1.000x
        public const int MIN_ATTACK_SPEED = 250;  // 0.250x
        public const int MAX_ATTACK_SPEED = 2000; // 2.000x

        // Crit
        public const int CRIT_CHANCE_SCALE = Scaling.BPS_SCALE;            // 10_000 = 100.00%
        public const int CRIT_DAMAGE_SCALE = Scaling.MULTIPLIER_PERMILLE; // 1000 = 1.000x
        public const int DEFAULT_CRIT_DAMAGE = 1500;                      // 1.500x (150%)

        // Cooldowns
        public const int BASE_ATTACK_COOLDOWN_TICKS = World.TICKS_PER_SECOND; // 1 segundo
        public const int MIN_ATTACK_COOLDOWN_TICKS = 1;

        // Limits
        public const int MAX_ATTACK_REQUESTS_PER_TICK = 100;

        // Growth
        public const int GROWTH_SCALE = Scaling.GROWTH_SCALE;

        public static class CombatStats
        {
            // Regras derivadas (tudo int)
            public const int PHYSICAL_ATTACK_PER_STR = 2;
            public const int MAGIC_ATTACK_PER_INT = 2;
            public const int PHYSICAL_DEFENSE_PER_CON = 1;
            public const int MAGIC_DEFENSE_PER_SPR = 1;

            // Multiplicador de attack speed (permille): +2 => +0.002x por DEX
            public const int ATTACK_SPEED_PERMILLE_PER_DEX = 2;

            // Chance crítico em BPS: +10 => +0.10% por DEX
            public const int CRIT_CHANCE_BPS_PER_DEX = 10;
        }
        
    }
}