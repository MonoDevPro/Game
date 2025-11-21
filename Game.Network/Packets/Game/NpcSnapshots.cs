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
    int PositionZ,
    int FacingX,
    int FacingY,
    int Hp,
    int MaxHp,
    float HpRegen,
    int PhysicalAttack,
    int MagicAttack,
    int PhysicalDefense,
    int MagicDefense);
    
[MemoryPackable]
public readonly partial record struct NpcStateSnapshot(
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
public readonly partial record struct NpcHealthSnapshot(
    int NetworkId,
    int CurrentHp,
    int MaxHp);
