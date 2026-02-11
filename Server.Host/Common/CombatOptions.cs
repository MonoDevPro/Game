namespace Server.Host.Common;

public sealed class CombatOptions
{
    public const string SectionName = "Combat";

    public CooldownOptions Cooldown { get; set; } = new();
    public StatsOptions Stats { get; set; } = new();
    public Dictionary<string, VocationOptions> Vocations { get; set; } = new();

    public sealed class CooldownOptions
    {
        public int MsPerAgility { get; set; } = 5;
        public float MinFactor { get; set; } = 0.60f;
    }

    public sealed class StatsOptions
    {
        public int BaseHpPerLevel { get; set; } = 5;
        public int BaseMpPerLevel { get; set; } = 3;
        public int HpPerVitality { get; set; } = 10;
        public int MpPerWillpower { get; set; } = 8;
    }

    public sealed class VocationOptions
    {
        // Stats Base
        public int BaseHp { get; set; } = 100;
        public int BaseMp { get; set; } = 50;
        public int HpGrowthPerLevel { get; set; } = 8;
        public int MpGrowthPerLevel { get; set; } = 4;
        public float HpStatScale { get; set; } = 1.0f;
        public float MpStatScale { get; set; } = 1.0f;

        // Combat Attack
        public int BaseCooldownMs { get; set; } = 1000;
        public int ManaCost { get; set; }
        public int Range { get; set; } = 1;
        // Velocidade em células por segundo.
        public float ProjectileSpeed { get; set; }
        public int DamageBase { get; set; } = 10;
        public float DamageScale { get; set; } = 1.0f;
        public string DamageStat { get; set; } = "Strength";

        public bool UsesProjectile => ProjectileSpeed > 0f;
    }
}
