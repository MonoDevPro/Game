using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Infrastructure.ArchECS.Services.Events;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Map;

namespace Game.Infrastructure.ArchECS.Services.Navigation.Systems;

/// <summary>
/// Sistema de movimento direcional (manual) do servidor.
/// Processa movimentos baseados em input de direção (WASD, setas, etc).
/// Não usa pathfinding - move diretamente na direção especificada.
/// </summary>
public sealed partial class NavDirectionalMovementSystem(World world, WorldMap grid) 
    : BaseSystem<World, long>(world)
{
    [Query]
    [All<Position, NavMovementState, NavAgent, NavDirectionalMode>]
    private void CompleteMovements(
        [Data] in long serverTick,
        in Entity entity,
        ref Position pos,
        ref FloorId floor,
        ref NavMovementState movement)
    {
        if (!movement.IsMoving)
            return;

        if (!movement.ShouldComplete(serverTick)) 
            return;
        
        // Atualiza posição para célula destino
        pos = movement.TargetCell;
        movement.Complete();
            
        MoveEvent moveEvent = new(entity, pos, floor.Value);
        EventBus.Send(ref moveEvent);
            
        if (World.Has<NavIsMoving>(entity))
            World.Remove<NavIsMoving>(entity);

        // Libera modo direcional ao finalizar o passo para permitir
        // que novas requests (single/continuous) sejam processadas.
        if (World.Has<NavDirectionalMode>(entity))
            World.Remove<NavDirectionalMode>(entity);
    }
    
    /// <summary>
    /// Processa novas requisições de movimento direcional.
    /// </summary>
    [Query]
    [All<Position, NavMovementState, NavAgentSettings, NavAgent, NavDirectionalRequest>]
    [None<NavIsMoving, NavDirectionalMode>]
    private void ProcessDirectionalRequests(
        [Data] in long serverTick,
        in Entity entity,
        ref Position pos,
        ref FloorId floor,
        ref NavMovementState movement,
        ref NavAgentSettings settings,
        ref NavDirectionalRequest request)
    {
        // Só processa movimento único
        if (request.MovementType != DirectionalMovementType.Single)
            return;
        
        // Se ainda está movendo, espera completar
        if (movement.IsMoving)
            return;

        TryStartDirectionalMove(
            entity,
            serverTick,
            pos,
            floor.Value,
            ref movement,
            ref settings,
            ref request,
            removeRequestOnFail: true,
            removeRequestOnSuccess: true,
            clearReachedDestination: true);
    }

    /// <summary>
    /// Processa movimento contínuo - re-aplica direção quando movimento anterior termina.
    /// </summary>
    [Query]
    [All<Position, NavMovementState, NavAgentSettings, NavAgent, NavDirectionalRequest>]
    [None<NavIsMoving, NavDirectionalMode>]
    private void ProcessContinuousMovement(
        [Data] in long serverTick,
        in Entity entity,
        ref Position pos,
        ref FloorId floor,
        ref NavMovementState movement,
        ref NavAgentSettings settings,
        ref NavDirectionalRequest request)
    {
        // Só processa movimento contínuo
        if (request.MovementType != DirectionalMovementType.Continuous)
            return;

        TryStartDirectionalMove(
            entity,
            serverTick,
            pos,
            floor.Value,
            ref movement,
            ref settings,
            ref request,
            removeRequestOnFail: false,
            removeRequestOnSuccess: false,
            clearReachedDestination: false);
    }

    private bool TryStartDirectionalMove(
        Entity entity,
        long serverTick,
        Position pos,
        int floor,
        ref NavMovementState movement,
        ref NavAgentSettings settings,
        ref NavDirectionalRequest request,
        bool removeRequestOnFail,
        bool removeRequestOnSuccess,
        bool clearReachedDestination)
    {
        var targetPos = new Position
        {
            X = pos.X + request.Direction.X,
            Y = pos.Y + request.Direction.Y
        };

        if (!grid.InBounds(targetPos.X, targetPos.Y, floor))
        {
            if (removeRequestOnFail)
                World.Remove<NavDirectionalRequest>(entity);
            return false;
        }

        if (!grid.TryMoveEntity(pos, floor, targetPos, floor, entity))
        {
            HandleBlockedMove(entity, serverTick, ref request, targetPos, floor);

            if (removeRequestOnFail)
                World.Remove<NavDirectionalRequest>(entity);

            return false;
        }

        bool isDiagonal = request.Direction.X != 0 && request.Direction.Y != 0;
        int duration = settings.GetDuration(isDiagonal);

        movement.StartMove(pos, targetPos, serverTick, duration);

        World.Add<NavIsMoving, NavDirectionalMode>(entity);

        if (World.Has<NavWaitingToMove>(entity))
            World.Remove<NavWaitingToMove>(entity);

        if (clearReachedDestination && World.Has<NavReachedDestination>(entity))
            World.Remove<NavReachedDestination>(entity);

        if (removeRequestOnSuccess)
            World.Remove<NavDirectionalRequest>(entity);

        return true;
    }

    private void HandleBlockedMove(
        Entity entity, 
        long serverTick, 
        ref NavDirectionalRequest request, 
        Position blockedCell, 
        int floor)
    {
        // Verifica se há entidade bloqueando
        if (!grid.TryGetFirstEntity(blockedCell, floor, out var blockingEntity))
        {
            // Obstáculo estático
            if (!World.Has<NavWaitingToMove>(entity))
            {
                World.Add(entity, new NavWaitingToMove
                {
                    WaitStartTick = serverTick,
                    BlockedByEntityId = -1
                });
            }
        }
        else
        {
            // Bloqueado por entidade
            if (!World.Has<NavWaitingToMove>(entity))
            {
                World.Add(entity, new NavWaitingToMove
                {
                    WaitStartTick = serverTick,
                    BlockedByEntityId = blockingEntity.Id
                });
            }
        }
    }
}
