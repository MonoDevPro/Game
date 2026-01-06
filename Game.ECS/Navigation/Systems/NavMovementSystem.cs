using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Navigation.Components;
using Game.ECS.Navigation.Core;
using Game.ECS.Navigation.Core.Contracts;

namespace Game.ECS.Navigation.Systems;

/// <summary>
/// Sistema de movimento do SERVIDOR para navegação.
/// Baseado em ticks, determinístico, autoritativo.
/// Integra com INavigationGrid (que usa MapSpatial para ocupação).
/// </summary>
public sealed partial class NavMovementSystem(World world, INavigationGrid grid) : BaseSystem<World, long>(world)
{
    [Query]
    [All<Position, NavMovementState, NavAgent>]
    private void CompleteMovements(
        [Data] in long serverTick,
        in Entity entity,
        ref Position pos,
        ref NavMovementState movement)
    {
        if (!movement.IsMoving)
            return;

        if (movement.ShouldComplete(serverTick))
        {
            // Atualiza posição para célula destino
            pos = movement.TargetCell;
            movement.Complete();
        }
    }

    [Query]
    [All<Position, NavMovementState, NavAgentSettings, NavPathBuffer, NavPathState, NavAgent>]
    private void ProcessPathFollowing(
        [Data] in long serverTick,
        in Entity entity,
        ref Position pos,
        ref NavMovementState movement,
        ref NavPathBuffer path,
        ref NavPathState state,
        ref NavAgentSettings settings)
    {
        // Só processa se tem caminho pronto ou seguindo
        if (state.Status != PathStatus.InProgress)
            return;

        // Ainda está movendo? Espera
        if (movement.IsMoving)
            return;

        // Caminho completo?
        if (!path.IsValid || path.IsComplete)
        {
            CompleteNavigation(World, entity, ref state, ref movement);
            return;
        }

        // Pega próximo waypoint
        var nextPos = path.GetCurrentWaypointAsPosition(grid.Width, grid.Height, pos.Z);

        if (nextPos.X < 0)
        {
            CompleteNavigation(World, entity, ref state, ref movement);
            return;
        }

        // Já está na posição?
        if (pos.Equals(nextPos))
        {
            path.AdvanceWaypoint();
            return;
        }

        // Tenta mover ocupação para próxima célula
        if (!grid.TryMoveOccupancy(pos, nextPos, entity))
        {
            // Célula ocupada ou bloqueada
            HandleBlockedMovement(World, entity, serverTick, ref state, ref movement, nextPos);
            return;
        }

        // Inicia movimento
        bool isDiagonal = pos.X != nextPos.X && pos.Y != nextPos.Y;
        int duration = settings.GetDuration(isDiagonal);

        movement.StartMove(pos, nextPos, serverTick, duration);
        path.AdvanceWaypoint();

        // Adiciona tag NavIsMoving
        if (!World.Has<NavIsMoving>(entity))
            World.Add<NavIsMoving>(entity);

        // Remove tags de espera
        if (World.Has<NavWaitingToMove>(entity))
            World.Remove<NavWaitingToMove>(entity);

        if (World.Has<NavReachedDestination>(entity))
            World.Remove<NavReachedDestination>(entity);
    }

    private void HandleBlockedMovement(
        World world,
        Entity entity,
        long serverTick,
        ref NavPathState state,
        ref NavMovementState movement,
        Position blockedCell)
    {
        // Verifica quem está bloqueando
        int blockerId = grid.GetOccupant(blockedCell.X, blockedCell.Y, blockedCell.Z);

        if (!world.Has<NavWaitingToMove>(entity))
        {
            world.Add(entity, new NavWaitingToMove
            {
                WaitStartTick = serverTick,
                BlockedByEntityId = blockerId
            });
        }

        // TODO: Poderia implementar recálculo de path após N ticks de espera
    }

    private static void CompleteNavigation(
        World world,
        Entity entity,
        ref NavPathState state,
        ref NavMovementState movement)
    {
        state.Status = PathStatus.Completed;
        movement.Reset();

        if (world.Has<NavIsMoving>(entity))
            world.Remove<NavIsMoving>(entity);

        if (!world.Has<NavReachedDestination>(entity))
            world.Add<NavReachedDestination>(entity);
    }
}
