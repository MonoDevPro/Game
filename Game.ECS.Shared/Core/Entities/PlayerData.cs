using MemoryPack;

namespace Game.ECS.Shared.Core.Entities;

[MemoryPackable]
public readonly partial record struct PlayerData(
    int PlayerId, int NetworkId, int MapId,
    string Name, byte Gender, byte Vocation,
    int X, int Y, int Z, byte Direction,
    int Hp, int MaxHp, int HpRegen,
    int Mp, int MaxMp, int MpRegen,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense
);