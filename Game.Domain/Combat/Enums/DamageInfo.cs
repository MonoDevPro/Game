namespace GameECS.Shared.Combat.Data;

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