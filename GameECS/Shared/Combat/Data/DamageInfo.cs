using MemoryPack;

namespace GameECS.Shared.Combat.Data;

/// <summary>
/// Tipos de dano que podem ser aplicados.
/// </summary>
public enum DamageType : byte { Physical = 0, Magic = 1, True = 2 /* Ignora defesas */ }

/// <summary>
/// Resultado de um ataque.
/// </summary>
public enum AttackResult : byte
{
    None = 0,
    Hit,
    Miss,
    Critical,
    Blocked,
    Evaded,
    OutOfRange,
    OnCooldown,
    NoTarget,
    TargetDead,
    InsufficientMana
}