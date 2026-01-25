namespace Game.Infrastructure.ArchECS.Services.Combat;

/// <summary>
/// Configuração básica de combate (placeholder para expansão gradual).
/// </summary>
public sealed class CombatConfig
{
    public int MsPerAgility { get; init; } = 5;
    public float MinCooldownFactor { get; init; } = 0.60f;
    public Dictionary<byte, VocationAttackConfig> Vocations { get; init; } = new();

    public static CombatConfig Default { get; } = new();
    
    public bool TryGetVocation(byte vocation, out VocationAttackConfig config)
    {
        if (Vocations.TryGetValue(vocation, out var value) && value is not null)
        {
            config = value;
            return true;
        }

        config = null!;
        return false;
    }

    public sealed class VocationAttackConfig
    {
        public int BaseCooldownMs { get; init; }
        public int ManaCost { get; init; }
        public int Range { get; init; }
        public float ProjectileSpeed { get; init; }
        public int DamageBase { get; init; }
        public float DamageScale { get; init; }
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
