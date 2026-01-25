using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Infrastructure.ArchECS.Commons.Components;
using Game.Infrastructure.ArchECS.Events;
using Game.Infrastructure.ArchECS.Services.Map;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;

namespace Game.Infrastructure.ArchECS.Services.Navigation.Systems;

/// <summary>
/// Sistema de movimento do SERVIDOR para navegação.
/// Baseado em ticks, determinístico, autoritativo.
/// Integra com INavigationGrid (que usa MapSpatial para ocupação).
/// </summary>
public sealed partial class NavMovementSystem(World world, WorldMap grid) : BaseSystem<World, long>(world)
{
    [Query]
    [All<Position, NavMovementState, NavAgent>]
    private void CompleteMovements(
        [Data] in long serverTick,
        in Entity entity,
        ref Position pos,
        ref FloorId floor,
        ref NavMovementState movement)
    {
        if (!movement.IsMoving)
            return;

        if (movement.ShouldComplete(serverTick))
        {
            // Atualiza posição para célula destino
            pos = movement.TargetCell;
            movement.Complete();
            
            MoveEvent moveEvent = new(entity, pos, floor.Value);
            EventBus.Send(ref moveEvent);
            
            if (World.Has<NavIsMoving>(entity))
                World.Remove<NavIsMoving>(entity);
        }
    }

    [Query]
    [All<Position, NavMovementState, NavAgentSettings, NavPathBuffer, NavPathState, NavAgent>]
    [None<NavDirectionalMode>]
    private void ProcessPathFollowing(
        [Data] in long serverTick,
        in Entity entity,
        ref Position pos,
        ref Direction dir,
        ref FloorId floor,
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
        var nextPos = path.GetCurrentWaypointAsPosition(grid.Width, grid.Height);

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
        if (!grid.TryMoveEntity(pos, floor.Value, nextPos, floor.Value, entity))
        {
            // Célula ocupada ou bloqueada
            HandleBlockedMovement(entity, serverTick, ref state, ref movement, nextPos, floor.Value);
            return;
        }

        // Inicia movimento
        bool isDiagonal = pos.X != nextPos.X && pos.Y != nextPos.Y;
        int duration = settings.GetDuration(isDiagonal);

        movement.StartMove(pos, nextPos, serverTick, duration);
        
        dir = movement.MovementDirection;
        
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
        Entity entity,
        long serverTick,
        ref NavPathState state,
        ref NavMovementState movement,
        Position blockedCell,
        int floor)
    {
        // Verifica quem está bloqueando
        if (!grid.TryGetFirstEntity(blockedCell, floor, out var blockingEntity))
        {
            // Célula bloqueada, mas sem entidade? Pode ser obstáculo estático.
            // Apenas marca como esperando.
            if (!world.Has<NavWaitingToMove>(entity))
            {
                world.Add(entity, new NavWaitingToMove
                {
                    WaitStartTick = serverTick,
                    BlockedByEntityId = -1
                });
            }

            return;
        }

        // Marca como esperando, com referência à entidade bloqueadora
        if (!world.Has<NavWaitingToMove>(entity))
        {
            world.Add(entity, new NavWaitingToMove
            {
                WaitStartTick = serverTick,
                BlockedByEntityId = blockingEntity.Id
            });
        }
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