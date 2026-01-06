using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game. ECS.Navigation. Components;
using Game. ECS.Navigation. Core;
using Game.ECS.Services.Pathfinding;

namespace Game.ECS.Navigation.Systems;

/// <summary>
/// Sistema de movimento do SERVIDOR. 
/// Baseado em ticks, determinístico, autoritativo.
/// </summary>
public sealed partial class ServerMovementSystem(World world, NavigationGrid grid) : BaseSystem<World, long>(world)
{
    [Query]
    [All<GridPosition, MovementState, GridNavigationAgent>]
    private void CompleteMovements(
        [Data]in long serverTick,
        in Entity entity,
        ref GridPosition pos,
        ref MovementState movement)
    {
        if (! movement.IsMoving)
            return;

        if (movement.ShouldComplete(serverTick))
        {
            // Atualiza posição para célula destino
            pos = movement.TargetCell;
            movement. Complete();
        }
    }

    [Query]
    [All<GridPosition, MovementState, ServerAgentSettings, GridNavigationAgent>]
    private void ProcessPathFollowing([Data]in long serverTick, in Entity entity,
        ref GridPosition pos,
        ref MovementState movement,
        ref PathBuffer path,
        ref PathfindingRequest state,
        ref ServerAgentSettings settings)
    {
            // Só processa se tem caminho pronto ou seguindo
            if (state.Status != PathfindingStatus.InProgress && state.Status != PathfindingStatus.Pending)
                return;

            // Ainda está movendo?  Espera
            if (movement. IsMoving)
                return;

            // Caminho completo? 
            if (! path.IsValid || path.IsComplete)
            {
                CompleteNavigation(World, entity, ref state, ref movement);
                return;
            }

            state.Status = PathfindingStatus.InProgress;

            // Pega próximo waypoint
            var nextPos = path.GetCurrentWaypointAsPosition(grid. Width);

            if (nextPos. X < 0)
            {
                CompleteNavigation(World, entity, ref state, ref movement);
                return;
            }

            // Já está na posição? 
            if (pos == nextPos)
            {
                path. AdvanceWaypoint();
                return;
            }

            // Tenta mover ocupação para próxima célula
            int entityId = entity.Id;
            if (! grid.TryMoveOccupancy(pos.X, pos.Y, nextPos.X, nextPos.Y, entityId))
            {
                // Célula ocupada ou bloqueada
                HandleBlockedMovement(World, entity, ref state, ref movement, nextPos);
                return;
            }

            // Inicia movimento
            bool isDiagonal = pos.X != nextPos.X && pos. Y != nextPos. Y;
            int duration = settings.GetDuration(isDiagonal);

            movement.StartMove(pos, nextPos, serverTick, duration);
            path.AdvanceWaypoint();

            // Adiciona tag IsMoving
            if (!World.Has<IsMoving>(entity))
                World.Add<IsMoving>(entity);

            // Remove tags de espera
            if (World.Has<WaitingToMove>(entity))
                World.Remove<WaitingToMove>(entity);

            if (World.Has<ReachedDestination>(entity))
                World.Remove<ReachedDestination>(entity);
    }

    private void HandleBlockedMovement(
        World world,
        Entity entity,
        ref PathfindingRequest state,
        ref MovementState movement,
        GridPosition blockedCell)
    {
        // Verifica quem está bloqueando
        int blockerId = grid.GetOccupant(blockedCell. X, blockedCell.Y);

        if (! world.Has<WaitingToMove>(entity))
        {
            world.Add(entity, new WaitingToMove
            {
                WaitTime = 0,
                BlockedByEntityId = blockerId
            });
        }

        // Poderia implementar:  recálculo de path após N ticks de espera
    }

    private static void CompleteNavigation(
        World world,
        Entity entity,
        ref PathfindingRequest state,
        ref MovementState movement)
    {
        state.Status = PathfindingStatus.Completed;
        movement.Reset();

        if (world.Has<IsMoving>(entity))
            world.Remove<IsMoving>(entity);

        if (! world.Has<ReachedDestination>(entity))
            world.Add<ReachedDestination>(entity);
    }
}