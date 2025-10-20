using System.Runtime.InteropServices;
using MemoryPack;

namespace Game.ECS.Components;

[MemoryPackable]
public readonly partial record struct PlayerInputSnapshot(
    int PlayerId,
    sbyte InputX,
    sbyte InputY,
    InputFlags Flags);

[MemoryPackable]
public readonly partial record struct PlayerStateSnapshot(
    int PlayerId,
    int PositionX, 
    int PositionY, 
    int PositionZ, 
    int FacingX, 
    int FacingY, 
    float Speed);

[MemoryPackable]
public readonly partial record struct PlayerVitalsSnapshot(
    int PlayerId, 
    int CurrentHp, 
    int MaxHp, 
    int CurrentMp, 
    int MaxMp
);

[MemoryPackable]
public readonly partial record struct PlayerDespawnSnapshot(int NetworkId);