using Game.Domain.Enums;

namespace Game.Abstractions.Network;

/// <summary>
/// Flags de sincronização de rede (até 64 flags)
/// Autor: MonoDevPro
/// Data: 2025-10-07 17:53:19
/// </summary>
[Flags]
public enum SyncFlags : ulong
{
    None = 0,
    Position = 1UL << 0,
    Direction = 1UL << 1,
    Velocity = 1UL << 2,
    Health = 1UL << 3,
    Mana = 1UL << 4,
    Equipment = 1UL << 5,
    Stats = 1UL << 6,
    AnimationState = 1UL << 7,
    AnimationSpeed = 1UL << 8,
    
    // ========== AÇÕES AGRUPADAS (32-47) ==========
    Movement       = AnimationState | AnimationSpeed | Position | Velocity | Direction,
    Attributes     = Health | Mana | Stats,
    
    All = ulong.MaxValue
}