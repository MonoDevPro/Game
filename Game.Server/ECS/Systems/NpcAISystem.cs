using System;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class NpcAISystem(World world, ILogger<NpcAISystem>? logger = null)
    : GameSystem(world)
{
    [Query]
    [All<AIControlled, Position, NpcAIState, NpcBehavior, NpcTarget, NpcPatrol, DirtyFlags>]
    private void UpdateAiState(
        ref NpcAIState aiState,
        in Position position,
        in NpcBehavior behavior,
        in NpcTarget target,
        in NpcPatrol patrol,
        ref DirtyFlags dirty,
        [Data] float deltaTime)
    {
        aiState.Advance(deltaTime);

        switch (aiState.Current)
        {
            case NpcAIStateId.Idle:
            case NpcAIStateId.Patrolling:
            {
                if (target.HasTarget && behavior.Type != NpcBehaviorType.Passive)
                    Transition(ref aiState, NpcAIStateId.Chasing, ref dirty);
                break;
            }
            case NpcAIStateId.Chasing:
            {
                if (!target.HasTarget)
                {
                    Transition(ref aiState, NpcAIStateId.Returning, ref dirty);
                    break;
                }

                float attackRangeSq = behavior.AttackRange * behavior.AttackRange;
                if (target.DistanceSquared <= attackRangeSq)
                    Transition(ref aiState, NpcAIStateId.Attacking, ref dirty);
                break;
            }
            case NpcAIStateId.Attacking:
            {
                if (!target.HasTarget)
                {
                    Transition(ref aiState, NpcAIStateId.Returning, ref dirty);
                    break;
                }

                float leashRangeSq = behavior.LeashRange * behavior.LeashRange;
                if (target.DistanceSquared > leashRangeSq)
                {
                    Transition(ref aiState, NpcAIStateId.Returning, ref dirty);
                    break;
                }

                float attackRangeSq = behavior.AttackRange * behavior.AttackRange * 1.2f;
                if (target.DistanceSquared > attackRangeSq)
                    Transition(ref aiState, NpcAIStateId.Chasing, ref dirty);
                break;
            }
            case NpcAIStateId.Returning:
            {
                if (target.HasTarget)
                {
                    Transition(ref aiState, NpcAIStateId.Chasing, ref dirty);
                    break;
                }

                if (position.ManhattanDistance(patrol.HomePosition) <= 0)
                    Transition(ref aiState, NpcAIStateId.Idle, ref dirty);
                break;
            }
        }
    }

    private void Transition(ref NpcAIState state, NpcAIStateId targetState, ref DirtyFlags dirty)
    {
        if (!state.TrySetState(targetState))
            return;

        dirty.MarkDirty(DirtyComponentType.State);
        logger?.LogTrace("[NpcAI] State transitioned to {State}", targetState);
    }
}
