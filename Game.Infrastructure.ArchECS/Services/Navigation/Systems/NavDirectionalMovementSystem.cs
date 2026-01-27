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

        // Calcula posição alvo
        var targetPos = new Position
        {
            X = pos.X + request.Direction.X,
            Y = pos.Y + request.Direction.Y
        };

        // Verifica se a posição é válida
        if (!grid.InBounds(targetPos.X, targetPos.Y, floor.Value))
        {
            World.Remove<NavDirectionalRequest>(entity);
            return;
        }

        // Verifica se a célula está livre
        if (!grid.TryMoveEntity(pos, floor.Value, targetPos, floor.Value, entity))
        {
            HandleBlockedMove(entity, serverTick, ref request, targetPos, floor.Value);
            World.Remove<NavDirectionalRequest>(entity);
            return;
        }

        // Inicia movimento
        bool isDiagonal = request.Direction.X != 0 && request.Direction.Y != 0;
        int duration = settings.GetDuration(isDiagonal);

        movement.StartMove(pos, targetPos, serverTick, duration);

        // Adiciona tags apropriadas
        World.Add<NavIsMoving, NavDirectionalMode>(entity);

        // Remove tags de espera/destino
        if (World.Has<NavWaitingToMove>(entity))
            World.Remove<NavWaitingToMove>(entity);

        if (World.Has<NavReachedDestination>(entity))
            World.Remove<NavReachedDestination>(entity);

        // Para movimento único, remove a request após iniciar
        World.Remove<NavDirectionalRequest>(entity);
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

        // Calcula próxima posição
        var targetPos = new Position
        {
            X = pos.X + request.Direction.X,
            Y = pos.Y + request.Direction.Y
        };

        // Verifica limites
        if (!grid.InBounds(targetPos.X, targetPos.Y, floor.Value))
            return;

        // Tenta mover
        if (!grid.TryMoveEntity(pos, floor.Value, targetPos, floor.Value, entity))
        {
            HandleBlockedMove(entity, serverTick, ref request, targetPos, floor.Value);
            return;
        }

        // Inicia movimento
        bool isDiagonal = request.Direction.X != 0 && request.Direction.Y != 0;
        int duration = settings.GetDuration(isDiagonal);

        movement.StartMove(pos, targetPos, serverTick, duration);

        World.Add<NavIsMoving, NavDirectionalMode>(entity);

        if (World.Has<NavWaitingToMove>(entity))
            World.Remove<NavWaitingToMove>(entity);
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
