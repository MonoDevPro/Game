namespace Game.Infrastructure.ArchECS.Services.Combat;

/// <summary>
/// Configuração completa de combate com suporte a HP/MP por vocação.
/// </summary>
public sealed class CombatConfig
{
    public CooldownConfig Cooldown { get; init; } = new();
    public StatsConfig Stats { get; init; } = new();
    public Dictionary<byte, VocationConfig> Vocations { get; init; } = new();

    public static CombatConfig Default { get; } = new();

    public bool TryGetVocation(byte vocation, out VocationConfig config)
    {
        if (Vocations.TryGetValue(vocation, out var value) && value is not null)
        {
            config = value;
            return true;
        }

        config = null!;
        return false;
    }

    public sealed class CooldownConfig
    {
        public int MsPerAgility { get; init; } = 5;
        public float MinCooldownFactor { get; init; } = 0.60f;
    }

    public sealed class StatsConfig
    {
        public int BaseHpPerLevel { get; init; } = 5;
        public int BaseMpPerLevel { get; init; } = 3;
        public int HpPerVitality { get; init; } = 10;
        public int MpPerWillpower { get; init; } = 8;
    }

    public sealed class VocationConfig
    {
        // Stats Base
        public int BaseHp { get; init; } = 100;
        public int BaseMp { get; init; } = 50;
        public int HpGrowthPerLevel { get; init; } = 8;
        public int MpGrowthPerLevel { get; init; } = 4;
        public float HpStatScale { get; init; } = 1.0f;
        public float MpStatScale { get; init; } = 1.0f;

        // Combat Attack
        public int BaseCooldownMs { get; init; } = 1000;
        public int ManaCost { get; init; }
        public int Range { get; init; } = 1;
        public float ProjectileSpeed { get; init; }
        public int DamageBase { get; init; } = 10;
        public float DamageScale { get; init; } = 1.0f;
        public CombatDamageStat DamageStat { get; init; } = CombatDamageStat.Strength;

        public bool UsesProjectile => ProjectileSpeed > 0f;
    }
}

public enum CombatDamageStat : byte
{
    Strength = 1,
    Endurance = 2,
    Agility = 3,
    Intelligence = 4,
    Willpower = 5
}

public static class CombatFormulas
{
    /// <summary>
    /// MaxHP = BaseHp + (Level * HpGrowthPerLevel) + (Vitality * HpPerVitality * HpStatScale)
    /// </summary>
    public static int CalculateMaxHp(CombatConfig.VocationConfig vocation, CombatConfig.StatsConfig stats, int level, int vitality)
    {
        var levelBonus = level * vocation.HpGrowthPerLevel;
        var statBonus = (int)(vitality * stats.HpPerVitality * vocation.HpStatScale);
        return vocation.BaseHp + levelBonus + statBonus;
    }

    /// <summary>
    /// MaxMP = BaseMp + (Level * MpGrowthPerLevel) + (Willpower * MpPerWillpower * MpStatScale)
    /// </summary>
    public static int CalculateMaxMp(CombatConfig.VocationConfig vocation, CombatConfig.StatsConfig stats, int level, int willpower)
    {
        var levelBonus = level * vocation.MpGrowthPerLevel;
        var statBonus = (int)(willpower * stats.MpPerWillpower * vocation.MpStatScale);
        return vocation.BaseMp + levelBonus + statBonus;
    }

    /// <summary>
    /// Cooldown efetivo com redução por agilidade.
    /// </summary>
    public static int CalculateCooldown(CombatConfig.VocationConfig vocation, CombatConfig.CooldownConfig cooldown, int agility)
    {
        var reduction = agility * cooldown.MsPerAgility;
        var minCooldown = (int)(vocation.BaseCooldownMs * cooldown.MinCooldownFactor);
        return Math.Max(vocation.BaseCooldownMs - reduction, minCooldown);
    }
}