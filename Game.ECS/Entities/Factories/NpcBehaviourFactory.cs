using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Archetypes;
using Game.ECS.Entities.Data;

namespace Game.ECS.Entities.Factories;

public static partial class EntityFactory
{
    public static void SetupNpcBehaviourEntity(this World world, Entity entity, NpcBehaviorData data)
    {
        if ((NpcBehaviorType)data.BehaviorType == NpcBehaviorType.Aggressive)
            world.Set<NpcBehavior>(entity, CreateAggressive(data.VisionRange, data.AttackRange, data.LeashRange));
        else if ((NpcBehaviorType)data.BehaviorType == NpcBehaviorType.Defensive)
            world.Set<NpcBehavior>(entity, CreateDefensive(data.VisionRange, data.AttackRange, data.LeashRange));
        else if ((NpcBehaviorType)data.BehaviorType == NpcBehaviorType.Passive)
            world.Set<NpcBehavior>(entity, CreatePassive(data.PatrolRadius, data.IdleDurationMin, data.IdleDurationMax));
    }
    
    private static NpcBehavior CreateAggressive(float visionRange, float attackRange, float leashRange)
    {
        return new NpcBehavior
        {
            Type = NpcBehaviorType.Aggressive,
            VisionRange = visionRange,
            AttackRange = attackRange,
            LeashRange = leashRange,
            PatrolRadius = 0f,
            IdleDurationMin = 0f,
            IdleDurationMax = 0f
        };
    }
    
    private static NpcBehavior CreateDefensive(float visionRange, float attackRange, float leashRange)
    {
        return new NpcBehavior
        {
            Type = NpcBehaviorType.Defensive,
            VisionRange = visionRange,
            AttackRange = attackRange,
            LeashRange = leashRange,
            PatrolRadius = 0f,
            IdleDurationMin = 0f,
            IdleDurationMax = 0f
        };
    }
    
    private static NpcBehavior CreatePassive(float patrolRadius, float idleDurationMin, float idleDurationMax)
    {
        return new NpcBehavior
        {
            Type = NpcBehaviorType.Passive,
            VisionRange = 0f,
            AttackRange = 0f,
            LeashRange = 0f,
            PatrolRadius = patrolRadius,
            IdleDurationMin = idleDurationMin,
            IdleDurationMax = idleDurationMax
        };
    }
}