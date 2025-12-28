using Game.Domain.AI.Enums;
using Game.Domain.Enums;

namespace Game.Domain.AI.Data;

/// <summary>
/// Template para criação de NPCs.
/// </summary>
public sealed class NpcTemplate
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public EntityType Type { get; init; } = EntityType.Monster;
    public NpcSubType SubType { get; init; } = NpcSubType.Hostile;
    public int Level { get; init; } = 1;
    public int BaseHealth { get; init; } = 100;
    public int BaseDamage { get; init; } = 10;
    public int BaseDefense { get; init; } = 5;
    public int AggroRange { get; init; } = 8;
    public int AttackRange { get; init; } = 1;
    public float AttackSpeed { get; init; } = 1.0f;
    public float MovementSpeed { get; init; } = 1.0f;
    public NpcBehaviorType DefaultBehavior { get; init; } = NpcBehaviorType.Wander;
    public int WanderRadius { get; init; } = 5;
    public int RespawnDelayTicks { get; init; } = 300;
    public bool CanAttack { get; init; } = true;
}