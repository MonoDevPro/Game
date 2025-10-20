using System.Runtime.InteropServices;
using MemoryPack;

namespace Game.ECS.Components;

[MemoryPackable]
public readonly partial record struct PlayerInputSnapshot(
    int NetworkId,
    PlayerInput Input);

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
public readonly partial record struct PlayerDespawnSnapshot(int NetworkId);

[MemoryPackable]
public readonly partial record struct PlayerSnapshot(
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
    float Speed,
    int Hp,
    int Mp,
    int MaxHp,
    int MaxMp,
    float HpRegen,
    float MpRegen,
    int PhysicalAttack,
    int MagicAttack,
    int PhysicalDefense,
    int MagicDefense,
    double AttackSpeed,
    double MovementSpeed);