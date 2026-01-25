namespace Server.Host.Common;

public sealed class CombatOptions
{
    public const string SectionName = "Combat";

    public CooldownOptions Cooldown { get; set; } = new();
    public Dictionary<string, VocationAttackOptions> Vocations { get; set; } = new();

    public sealed class CooldownOptions
    {
        public int MsPerAgility { get; set; } = 5;
        public float MinFactor { get; set; } = 0.60f;
    }

    public sealed class VocationAttackOptions
    {
        public int BaseCooldownMs { get; set; }
        public int ManaCost { get; set; }
        public int Range { get; set; }
        public float ProjectileSpeed { get; set; }
        public int DamageBase { get; set; }
        public float DamageScale { get; set; }
        public string DamageStat { get; set; } = "Strength";
    }
}
