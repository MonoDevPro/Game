using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class NpcMovementSystem(World world) : GameSystem(world)
{
    [Query]
    [All<AIControlled, Input, Position, NpcAIState, NpcTarget, NpcPatrol, NpcBehavior, DirtyFlags>]
    private void DriveMovement(
        ref Input input,
        in Position position,
        ref Facing facing,
        ref DirtyFlags dirty,
        in NpcAIState aiState,
        in NpcTarget target,
        in NpcPatrol patrol,
        in NpcBehavior _)
    {
        Facing desiredDirection = default;
        
        input.Flags = InputFlags.None;

        switch (aiState.Current)
        {
            case NpcAIStateId.Chasing when target.HasTarget:
                desiredDirection = PositionLogic.GetDirectionTowards(position, target.LastKnownPosition);
                input.Flags |= InputFlags.Sprint;
                break;
            case NpcAIStateId.Returning:
                desiredDirection = PositionLogic.GetDirectionTowards(position, patrol.HomePosition);
                input.Flags |= InputFlags.Sprint;
                break;
            case NpcAIStateId.Patrolling when patrol.HasDestination:
                desiredDirection = PositionLogic.GetDirectionTowards(position, patrol.Destination);
                break;
            case NpcAIStateId.Attacking:
                desiredDirection = PositionLogic.GetDirectionTowards(position, target.LastKnownPosition);
                input.Flags |= InputFlags.Attack;
                break;
            case NpcAIStateId.Idle:
                
                break;
        }

        if (desiredDirection.DirectionX != 0 || desiredDirection.DirectionY != 0)
        {
            if ((input.Flags & InputFlags.Attack) != 0)
            {
                input.InputX = 0;
                input.InputY = 0;
                facing.DirectionX = desiredDirection.DirectionX;
                facing.DirectionY = desiredDirection.DirectionY;
                dirty.MarkDirty(DirtyComponentType.State);
            }
            else
            {
                input.InputX = desiredDirection.DirectionX;
                input.InputY = desiredDirection.DirectionY;
            }
        }
    }
}