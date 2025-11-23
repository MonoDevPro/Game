using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct NpcSpawnSnapshot(
    int NetworkId,
    int MapId,
    string Name,
    byte Gender,
    byte Vocation,
    int PositionX,
    int PositionY,
    sbyte Floor,
    sbyte FacingX,
    sbyte FacingY,
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
    int MagicDefense);
    
[MemoryPackable]
public readonly partial record struct NpcStateSnapshot(
    int NetworkId,
    int PositionX,
    int PositionY,
    sbyte Floor,
    sbyte VelocityX,
    sbyte VelocityY,
    float Speed,
    sbyte FacingX,
    sbyte FacingY);

[MemoryPackable]
public readonly partial record struct NpcHealthSnapshot(
    int NetworkId,
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp);
