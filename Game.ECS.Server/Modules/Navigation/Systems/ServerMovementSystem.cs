using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Server.Modules.Navigation.Components;
using Game.ECS.Shared.Components.Navigation;
using Game.ECS.Shared.Core.Navigation;

namespace Game.ECS.Server.Modules.Navigation.Systems;

/// <summary>
/// Sistema de movimento do servidor (tick-based, autoritativo).
/// </summary>
public sealed partial class ServerMovementSystem(World world, NavigationGrid grid) : BaseSystem<World, long>(world)
{
    [Query]
    [All<GridPosition, ServerMovement, NavigationAgent>]
    private void CompleteMovements([Data] in long tick, ref GridPosition pos, ref ServerMovement movement)
    {
        if (movement.IsMoving && movement.ShouldComplete(tick))
        {
            pos = movement.TargetCell;
            movement.Complete();
        }
    }

    [Query]
    [All<GridPosition, ServerMovement, GridPathBuffer, PathState, ServerAgentConfig, NavigationAgent>]
    private void ProcessPathFollowing([Data] in long tick, in Entity entity, ref GridPosition pos, 
        ref ServerMovement movement, ref GridPathBuffer path, ref PathState state, ref ServerAgentConfig config)
    {
        if (state.Status != PathStatus.Ready && state.Status != PathStatus.Following)
            return;

        if (movement.IsMoving) return;

        if (! path.IsValid || path.IsComplete)
        {
            FinishNavigation(entity, ref state, ref movement);
            return;
        }

        state.Status = PathStatus.Following;
        state.LastUpdateTick = tick;

        var nextPos = path.GetCurrentWaypointPosition(grid.Width);
        if (nextPos == GridPosition.Invalid)
        {
            FinishNavigation(entity, ref state, ref movement);
            return;
        }

        if (pos == nextPos)
        {
            path.TryAdvance();
            return;
        }

        // Tenta mover ocupação
        if (!grid.TryMoveOccupancy(pos, nextPos, entity.Id))
        {
            // Bloqueado
            if (!World.Has<WaitingForPath>(entity))
                World.Add(entity, new WaitingForPath { StartTick = tick, BlockerId = grid.GetOccupant(nextPos.X, nextPos.Y) });
            return;
        }

        // Inicia movimento
        bool diagonal = pos.X != nextPos.X && pos.Y != nextPos.Y;
        movement.Start(pos, nextPos, tick, config.GetMoveTicks(diagonal));
        path.TryAdvance();

        if (!World.Has<IsMoving>(entity))
            World.Add<IsMoving>(entity);

        if (World.Has<WaitingForPath>(entity))
            World.Remove<WaitingForPath>(entity);

        if (World.Has<ReachedDestination>(entity))
            World.Remove<ReachedDestination>(entity);
    }

    private void FinishNavigation(Entity entity, ref PathState state, ref ServerMovement movement)
    {
        state.Status = PathStatus.Completed;
        movement.Reset();

        if (World.Has<IsMoving>(entity))
            World.Remove<IsMoving>(entity);

        if (! World.Has<ReachedDestination>(entity))
            World.Add<ReachedDestination>(entity);
    }
}