using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class NpcMovementSystem(World world, ILogger<NpcMovementSystem>? logger = null) : GameSystem(world)
{
    [Query]
    [All<AIControlled, Input, Position, NpcAIState, NpcTarget, NpcPatrol, NpcBehavior, DirtyFlags>]
    private void DriveMovement(
        ref Input input,
        in Position position,
        in NpcAIState aiState,
        in NpcTarget target,
        in NpcPatrol patrol,
        in NpcBehavior behavior,
        ref DirtyFlags dirty)
    {
        sbyte desiredX = 0;
        sbyte desiredY = 0;

        switch (aiState.Current)
        {
            case NpcAIStateId.Chasing when target.HasTarget:
            {
                float attackRangeSq = behavior.AttackRange * behavior.AttackRange;
                if (target.DistanceSquared > attackRangeSq)
                {
                    (desiredX, desiredY) = ComputeDirection(position, target.LastKnownPosition);
                }
                break;
            }
            case NpcAIStateId.Returning:
            {
                (desiredX, desiredY) = ComputeDirection(position, patrol.HomePosition);
                break;
            }
            case NpcAIStateId.Patrolling when patrol.HasDestination:
            {
                (desiredX, desiredY) = ComputeDirection(position, patrol.Destination);
                break;
            }
        }

        bool inputChanged = desiredX != input.InputX || desiredY != input.InputY;
        if (inputChanged)
        {
            input.InputX = desiredX;
            input.InputY = desiredY;
            dirty.MarkDirty(DirtyComponentType.Input);
        }
    }

    private static (sbyte X, sbyte Y) ComputeDirection(in Position from, in Position to)
    {
        sbyte x = 0;
        sbyte y = 0;

        if (to.X > from.X) x = 1;
        else if (to.X < from.X) x = -1;

        if (to.Y > from.Y) y = 1;
        else if (to.Y < from.Y) y = -1;

        return (x, y);
    }
}
