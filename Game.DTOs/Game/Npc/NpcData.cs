using MemoryPack;

namespace Game.DTOs.Game.Npc;

[MemoryPackable]
public readonly partial record struct NpcData(
    int NpcId, int NetworkId, int MapId, string Name,
    int X, int Y, int Z, int DirX, int DirY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense
);