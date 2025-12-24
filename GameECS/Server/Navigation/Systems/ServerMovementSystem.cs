using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameECS.Server.Navigation.Components;
using GameECS.Shared.Navigation.Components;
using GameECS.Shared.Navigation.Core;

namespace GameECS.Server.Navigation.Systems;

/// <summary>
/// Sistema de movimento do servidor (tick-based, autoritativo).
/// </summary>
public sealed partial class ServerMovementSystem : BaseSystem<World, long>
{
    private readonly NavigationGrid _grid;
    private long _currentTick;

    public ServerMovementSystem(World world, NavigationGrid grid) : base(world)
    {
        _grid = grid;
    }

    public override void Update(in long tick)
    {
        _currentTick = tick;
        CompleteMovementsQuery(World);
        ProcessPathFollowingQuery(World);
    }

    [Query]
    [All<GridPosition, ServerMovement, NavigationAgent>]
    private void CompleteMovements(ref GridPosition pos, ref ServerMovement movement)
    {
        if (movement.IsMoving && movement.ShouldComplete(_currentTick))
        {
            pos = movement.TargetCell;
            movement.Complete();
        }
    }

    [Query]
    [All<GridPosition, ServerMovement, GridPathBuffer, PathState, ServerAgentConfig, NavigationAgent>]
    private void ProcessPathFollowing(
        Entity entity,
        ref GridPosition pos,
        ref ServerMovement movement,
        ref GridPathBuffer path,
        ref PathState state,
        ref ServerAgentConfig config)
    {
        if (state.Status != PathStatus.Ready && state.Status != PathStatus.Following)
            return;

        if (movement.IsMoving) return;

        if (!path.IsValid || path.IsComplete)
        {
            FinishNavigation(entity, ref state, ref movement);
            return;
        }

        state.Status = PathStatus.Following;
        state.LastUpdateTick = _currentTick;

        var nextPos = path.GetCurrentWaypointPosition(_grid.Width);
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
        if (!_grid.TryMoveOccupancy(pos, nextPos, entity.Id))
        {
            // Bloqueado
            if (!World.Has<WaitingForPath>(entity))
                World.Add(entity, new WaitingForPath { StartTick = _currentTick, BlockerId = _grid.GetOccupant(nextPos.X, nextPos.Y) });
            return;
        }

        // Inicia movimento
        bool diagonal = pos.X != nextPos.X && pos.Y != nextPos.Y;
        movement.Start(pos, nextPos, _currentTick, config.GetMoveTicks(diagonal));
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

        if (!World.Has<ReachedDestination>(entity))
            World.Add<ReachedDestination>(entity);
    }
}