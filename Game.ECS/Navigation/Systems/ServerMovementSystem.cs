using Arch.Core;
using Arch.System;
using Game. ECS.Navigation. Components;
using Game. ECS.Navigation. Core;

namespace Game.ECS.Navigation.Systems;

/// <summary>
/// Sistema de movimento do SERVIDOR. 
/// Baseado em ticks, determinístico, autoritativo.
/// </summary>
public sealed class ServerMovementSystem(World world, NavigationGrid grid) : BaseSystem<World, long>(world)
{
    public override void Update(in long serverTick)
    {
        // 1. Completa movimentos que terminaram
        CompleteMovements(World, serverTick);
        
        // 2. Inicia novos movimentos do path buffer
        ProcessPathFollowing(World, serverTick);
    }

    private void CompleteMovements(World world, long serverTick)
    {
        var query = new QueryDescription()
            .WithAll<GridPosition, ServerMovementState, GridNavigationAgent>();

        world.Query(in query, (Entity entity,
            ref GridPosition pos,
            ref ServerMovementState movement) =>
        {
            if (! movement.IsMoving)
                return;

            if (movement.ShouldComplete(serverTick))
            {
                // Atualiza posição para célula destino
                pos = movement.TargetCell;
                movement. Complete();
            }
        });
    }

    private void ProcessPathFollowing(World world, long serverTick)
    {
        var query = new QueryDescription()
            .WithAll<GridPosition, ServerMovementState, GridPathBuffer, 
                     PathState, ServerAgentSettings, GridNavigationAgent>();

        world.Query(in query, (Entity entity,
            ref GridPosition pos,
            ref ServerMovementState movement,
            ref GridPathBuffer path,
            ref PathState state,
            ref ServerAgentSettings settings) =>
        {
            // Só processa se tem caminho pronto ou seguindo
            if (state.Status != PathStatus.Ready && state.Status != PathStatus.Following)
                return;

            // Ainda está movendo?  Espera
            if (movement. IsMoving)
                return;

            // Caminho completo? 
            if (! path.IsValid || path.IsComplete)
            {
                CompleteNavigation(world, entity, ref state, ref movement);
                return;
            }

            state.Status = PathStatus.Following;

            // Pega próximo waypoint
            var nextPos = path.GetCurrentWaypointAsPosition(grid. Width);

            if (nextPos. X < 0)
            {
                CompleteNavigation(world, entity, ref state, ref movement);
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
                HandleBlockedMovement(world, entity, ref state, ref movement, nextPos);
                return;
            }

            // Inicia movimento
            bool isDiagonal = pos.X != nextPos.X && pos. Y != nextPos. Y;
            int duration = settings.GetDuration(isDiagonal);

            movement.StartMove(pos, nextPos, serverTick, duration);
            path.AdvanceWaypoint();

            // Adiciona tag IsMoving
            if (! world.Has<IsMoving>(entity))
                world.Add<IsMoving>(entity);

            // Remove tags de espera
            if (world.Has<WaitingToMove>(entity))
                world.Remove<WaitingToMove>(entity);

            if (world.Has<ReachedDestination>(entity))
                world.Remove<ReachedDestination>(entity);
        });
    }

    private void HandleBlockedMovement(
        World world,
        Entity entity,
        ref PathState state,
        ref ServerMovementState movement,
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
        ref PathState state,
        ref ServerMovementState movement)
    {
        state.Status = PathStatus. Completed;
        movement.Reset();

        if (world.Has<IsMoving>(entity))
            world.Remove<IsMoving>(entity);

        if (! world.Has<ReachedDestination>(entity))
            world.Add<ReachedDestination>(entity);
    }
}