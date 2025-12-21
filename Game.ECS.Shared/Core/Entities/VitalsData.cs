using MemoryPack;

namespace Game.ECS.Shared.Core.Entities;

[MemoryPackable]
public readonly partial record struct VitalsData(
    int Id,
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp
);