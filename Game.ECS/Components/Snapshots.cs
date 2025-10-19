using System.Runtime.InteropServices;
using MemoryPack;

namespace Game.ECS.Components;

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct GameSnapshot(
    MapSnapshot MapSnapshot,
    PlayerSnapshot LocalPlayer,
    PlayerSnapshot[] OtherPlayers);

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct MapSnapshot(
    string Name,
    int Width,
    int Height,
    byte[] TileData,      // Row-major: index = y * Width + x
    byte[] CollisionData  // Row-major: index = y * Width + x
);

/// <summary>
/// Flat representation of a player's visible state for sync packets.
/// </summary>
[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct PlayerSnapshot(
    int NetworkId,
    int PlayerId,
    int CharacterId,
    string Name,
    byte Gender,
    byte Vocation,
    int PositionX,
    int PositionY,
    int PositionZ,
    int FacingX,
    int FacingY,
    float Speed);

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct PlayerInputSnapshot(
    int PlayerId,
    sbyte InputX,
    sbyte InputY,
    InputFlags Flags);

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct PlayerStateSnapshot(
    int PlayerId,
    int PositionX, 
    int PositionY, 
    int PositionZ, 
    int FacingX, 
    int FacingY, 
    float Speed);

[MemoryPackable]
public readonly record struct PlayerVitalsSnapshot(
    int PlayerId, 
    int CurrentHp, 
    int MaxHp, 
    int CurrentMp, 
    int MaxMp
);

[MemoryPackable]
public readonly record struct PlayerDespawnSnapshot(int NetworkId);