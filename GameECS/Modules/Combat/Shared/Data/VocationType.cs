namespace GameECS.Modules.Combat.Shared.Data;

/// <summary>
/// Tipos de vocação disponíveis no jogo.
/// </summary>
public enum VocationType : byte
{
    None = 0,
    Knight = 1,
    Mage = 2,
    Archer = 3
}

/// <summary>
/// Tipos de dano que podem ser aplicados.
/// </summary>
public enum DamageType : byte
{
    Physical = 0,
    Magic = 1,
    True = 2  // Ignora defesas
}

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
