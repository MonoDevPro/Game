using Game.ECS.Shared.Components.Navigation;
using MemoryPack;

namespace Game.ECS.Shared.Core.Entities;

[MemoryPackable]
public readonly partial record struct PlayerData(
    int Id, string Name, byte Gender, byte Vocation,
    int X, int Y, int Z, MovementDirection Direction,
    int Hp, int MaxHp, int Mp, int MaxMp,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense
);