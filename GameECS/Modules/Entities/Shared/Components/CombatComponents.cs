namespace GameECS.Modules.Entities.Shared.Components;

// Dead e Health já existem em GameECS.Modules.Combat.Shared.Components

/// <summary>
/// Tag: Entidade em combate.
/// </summary>
public struct InCombatState
{
    public long CombatStartTick;
    public long LastCombatActionTick;
}

/// <summary>
/// Tag: Entidade invulnerável.
/// </summary>
public struct Invulnerable { }
