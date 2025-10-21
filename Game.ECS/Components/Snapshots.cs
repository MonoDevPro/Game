using Game.ECS.Entities;
using MemoryPack;

namespace Game.ECS.Components;

[MemoryPackable]
public readonly partial record struct PlayerInputSnapshot(
    int NetworkId,
    sbyte InputX, 
    sbyte InputY, 
    InputFlags Flags);

[MemoryPackable]
public readonly partial record struct PlayerStateSnapshot(
    int NetworkId,
    int PositionX, 
    int PositionY, 
    int PositionZ, 
    int FacingX, 
    int FacingY, 
    float Speed);

[MemoryPackable]
public readonly partial record struct PlayerVitalsSnapshot(
    int NetworkId, 
    int CurrentHp, 
    int MaxHp, 
    int CurrentMp, 
    int MaxMp
);

[MemoryPackable]
public readonly partial record struct PlayerDespawn(int NetworkId);
