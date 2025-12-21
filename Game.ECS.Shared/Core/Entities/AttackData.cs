using Game.ECS.Shared.Components.Combat;
using MemoryPack;

namespace Game.ECS.Shared.Core.Entities;

[MemoryPackable]
public readonly partial record struct AttackData(
    int AttackerId,
    AttackStyle Style,
    float AttackDuration,
    float CooldownRemaining);