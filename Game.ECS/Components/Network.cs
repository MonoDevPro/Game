using System.Runtime.InteropServices;
using Game.ECS.Systems;

namespace Game.ECS.Components;

/// <summary>
/// Marca entidade como dirty para sincronização
/// Autor: MonoDevPro
/// Data: 2025-10-07 17:53:19
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NetworkDirty { public ulong Flags; public uint LastProcessedInputSequence; }

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
    
    // ========== AÇÕES AGRUPADAS (32-47) ==========
    Movement       = Position | Velocity | Direction,
    Vitals         = Health | Mana,
    
    // ✅ Apenas as flags que você REALMENTE usa
    InitialSync = Movement | Vitals,
}