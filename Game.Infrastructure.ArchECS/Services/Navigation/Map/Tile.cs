using System.Runtime.InteropServices;

namespace Game.Infrastructure.ArchECS.Services.Navigation.Map;

/// <summary>
/// Tile compacto (8 bytes alinhados para performance).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Tile
{
    public ushort TypeId;        // 2 bytes - Tipo do tile (grama, água, etc)
    public byte CollisionMask;   // 1 byte  - 0=livre, bits para direções bloqueadas
    public byte MovementCost;    // 1 byte  - Custo de movimento (1-255, 0=intransponível)
    public uint Flags;           // 4 bytes - Flags extras (PvP zone, safe zone, etc)
    
    public readonly bool IsBlocked => CollisionMask != 0 || MovementCost == 0;
    public readonly float Cost => MovementCost == 0 ? float.MaxValue : MovementCost / 10f;
    
    public static readonly Tile Empty = new() { MovementCost = 10 }; // Custo 1. 0
    public static readonly Tile Blocked = new() { CollisionMask = 1, MovementCost = 0 };
}