using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct PlayerSnapshot(
    int PlayerId,
    int NetworkId,
    string Name,
    byte Gender,
    byte Vocation,
    int PositionX,
    int PositionY,
    int PositionZ,
    int FacingX,
    int FacingY,
    int Hp,
    int MaxHp,
    float HpRegen,
    int Mp,
    int MaxMp,
    float MpRegen,
    float MovementSpeed,
    float AttackSpeed,
    int PhysicalAttack,
    int MagicAttack,
    int PhysicalDefense,
    int MagicDefense,
    int MapId);
    
[MemoryPackable]
public readonly partial record struct PlayerStateSnapshot(
    int NetworkId,
    int PositionX,
    int PositionY,
    int PositionZ,
    int VelocityX,
    int VelocityY,
    float Speed,
    int FacingX,
    int FacingY);
    
[MemoryPackable]
public readonly partial record struct PlayerVitalsSnapshot(
    int NetworkId,
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp);