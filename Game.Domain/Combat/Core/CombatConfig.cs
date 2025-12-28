namespace GameECS.Shared.Combat.Data;

/// <summary>
/// Configuração global do sistema de combate.
/// </summary>
public sealed class CombatConfig
{
    /// <summary>
    /// Cooldown base de ataque em ticks (antes de modificadores).
    /// </summary>
    public int BaseAttackCooldownTicks { get; init; } = 60; // 1 segundo @ 60 ticks/s

    /// <summary>
    /// Multiplicador de dano crítico.
    /// </summary>
    public double CriticalDamageMultiplier { get; init; } = 1.5f;

    /// <summary>
    /// Chance base de crítico (0-100).
    /// </summary>
    public double BaseCriticalChance { get; init; } = 5f;

    /// <summary>
    /// Máximo de requisições de ataque processadas por tick.
    /// </summary>
    public int MaxAttackRequestsPerTick { get; init; } = 100;

    /// <summary>
    /// Se deve permitir friendly fire.
    /// </summary>
    public bool AllowFriendlyFire { get; init; } = false;

    public static CombatConfig Default => new();

    public static CombatConfig PvPBalanced => new()
    {
        BaseAttackCooldownTicks = 45,
        CriticalDamageMultiplier = 1.3f,
        BaseCriticalChance = 3f,
        AllowFriendlyFire = false
    };

    public static CombatConfig PvE => new()
    {
        BaseAttackCooldownTicks = 30,
        CriticalDamageMultiplier = 2f,
        BaseCriticalChance = 10f,
        AllowFriendlyFire = false
    };
}
