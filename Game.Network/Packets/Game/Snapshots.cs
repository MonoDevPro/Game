using Game.ECS.Schema.Components;
using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct InputData(
    sbyte InputX, sbyte InputY, 
    InputFlags Flags
);

[MemoryPackable]
public readonly partial record struct StateData(
    int NetworkId,
    int X, int Y, sbyte Floor,
    float Speed,
    sbyte DirX, sbyte DirY
);
    
[MemoryPackable]
public readonly partial record struct VitalsData(
    int NetworkId,
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp
);

[MemoryPackable]
public readonly partial record struct PlayerData(
    int PlayerId, int NetworkId, int MapId,
    string Name, byte Gender, byte Vocation,
    int X, int Y, sbyte Floor,
    sbyte DirX, sbyte DirY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense
);

[MemoryPackable]
public readonly partial record struct NpcData(
    int NpcId, int NetworkId, int MapId,
    string Name, byte Gender, byte Vocation,
    int X, int Y, sbyte Floor,
    sbyte DirX, sbyte DirY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense
);