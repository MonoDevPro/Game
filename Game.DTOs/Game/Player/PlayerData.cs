using MemoryPack;

namespace Game.DTOs.Game.Player;

[MemoryPackable]
public readonly partial record struct PlayerData(
    int PlayerId, int NetworkId, int MapId,
    string Name, byte Gender, byte Vocation,
    int X, int Y, int Z,
    sbyte DirX, sbyte DirY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense
);