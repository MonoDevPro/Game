namespace Simulation.Components;

/// <summary>
/// Flags de estado que mudam frequentemente (1 byte).
/// Use para inputs/estados transitórios (move, attack, etc).
/// </summary>
[Flags]
public enum StateFlagsEnum : byte
{
    None      = 0,
    MoveUp    = 1 << 0,
    MoveDown  = 1 << 1,
    MoveLeft  = 1 << 2,
    MoveRight = 1 << 3,
    Attack    = 1 << 4,

    // Reserve bits: 1 << 5, 1 << 6, 1 << 7 (extras futuros)
}

/// <summary>
/// Traits / classification do alvo (32 bits).
/// Use para características mais estáveis (tipo, tags, etc).
/// </summary>
[Flags]
public enum TargetFlagsEnum : uint
{
    None    = 0u,
    Player  = 1u << 0,  // jogador
    NPC     = 1u << 1,  // NPC/merchant/etc
    Monster = 1u << 2,  // inimigo/monstro

    // Ex.: reserve bits para expandir sem quebrar rede
    // Boss   = 1u << 3,
    // Elite  = 1u << 4,
    // Summoned = 1u << 5,
}

// Componente simples para armazenar flags (por entidade)
public struct StateFlags { public StateFlagsEnum FlagsEnum; }

public struct TargetFlags { public TargetFlagsEnum FlagsEnum; }