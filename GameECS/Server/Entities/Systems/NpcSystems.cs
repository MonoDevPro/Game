using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Domain.AI.Enums;
using Game.Domain.AI.ValueObjects;
using Game.Domain.Combat.ValueObjects;
using Game.Domain.Commons.ValueObjects.Map;

namespace GameECS.Systems;

/// <summary>
/// Sistema de IA para NPCs.
/// </summary>
public sealed partial class NpcAISystem(World world) : BaseSystem<World, long>(world)
{
    private readonly Random _random = new();

    [Query]
    [All<NpcAI, NpcBehavior, GridPosition>, None<Dead>]
    private void ProcessIdleNpcs([Data] in long tick, in Entity entity, ref NpcAI ai, ref NpcBehavior behavior, ref GridPosition position)
    {
        if (ai.State != NpcAIState.Idle) return;
        if (tick < ai.NextActionTick) return;

        // Wander aleatório
        if (behavior.WanderRadius > 0 && behavior.Type != NpcBehaviorType.Stationary)
        {
            ai.State = NpcAIState.Wandering;
            ai.StateChangeTick = tick;
            ai.NextActionTick = tick + _random.Next(200, 500);
        }
    }

    [Query]
    [All<NpcAI, NpcBehavior, GridPosition, AggroTable>, None<Dead>]
    private void ProcessChasingNpcs([Data] in long tick, in Entity entity, ref NpcAI ai, ref NpcBehavior behavior, ref GridPosition position, ref AggroTable aggro)
    {
        if (ai.State != NpcAIState.Chasing) return;
        if (ai.TargetEntityId == 0)
        {
            ai.State = NpcAIState.Returning;
            ai.StateChangeTick = tick;
        }
    }

    [Query]
    [All<NpcAI, SpawnInfo, GridPosition>, None<Dead>]
    private void ProcessReturningNpcs([Data] in long tick, in Entity entity, ref NpcAI ai, ref SpawnInfo spawn, ref GridPosition position)
    {
        if (ai.State != NpcAIState.Returning) return;

        // Verifica se chegou ao spawn
        if (position.X == spawn.SpawnX && position.Y == spawn.SpawnY)
        {
            ai.State = NpcAIState.Idle;
            ai.StateChangeTick = tick;
            ai.NextActionTick = tick + 100;
        }
    }
}