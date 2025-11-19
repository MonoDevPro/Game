using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct NpcSpawnSnapshot(
    int NetworkId,
    int MapId,
    int PositionX,
    int PositionY,
    int PositionZ,
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
    int FacingX,
    int FacingY,
    float Speed,
    int CurrentHp,
    int MaxHp);