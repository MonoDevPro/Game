using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Events;
using Game.ECS.Services.Map;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

public sealed partial class MovementSystem(
    World world,
    MapIndex mapIndex,
    GameEventBus eventBus,
    ILogger<MovementSystem>? logger = null) : GameSystem(world, logger)
{
    [Query]
    [All<NavigationAgent, NavigationPath>]
    private void ProcessNavigationAgents(
        in Entity entity,
        ref NavigationAgent agent,
        ref NavigationPath path,
        [Data] float deltaTime)
    {
        // Se não há caminho, nada a fazer
        if (path.Waypoints.Length == 0 || path.CurrentWaypointIndex >= path.Waypoints.Length)
            return;

        // Obtém a posição alvo atual
        var targetPos = path.Waypoints[path.CurrentWaypointIndex];

        // Move o agente em direção ao waypoint
        var direction = new Direction
        {
            X = (sbyte)(targetPos.X - agent.CurrentPosition.X),
            Y = (sbyte)(targetPos.Y - agent.CurrentPosition.Y)
        };

        // Normaliza a direção
        var length = MathF.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        if (length > 0f)
        {
            direction.X /= (sbyte)length;
            direction.Y /= (sbyte)length;
        }

        // Atualiza a posição atual do agente
        agent.CurrentPosition.X += (int)(direction.X * agent.Speed * deltaTime);
        agent.CurrentPosition.Y += (int)(direction.Y * agent.Speed * deltaTime);

        // Verifica se alcançou o waypoint
        if (MathF.Abs(agent.CurrentPosition.X - targetPos.X) < 0.1f &&
            MathF.Abs(agent.CurrentPosition.Y - targetPos.Y) < 0.1f)
        {
            // Move para o próximo waypoint
            path.CurrentWaypointIndex++;
        }
    }
    
    [Query]
    [All<Position, Direction, Speed, Walkable>]
    [None<Dead, MovementIntent>]
    private void GenerateIntent(
        in Entity entity,
        in Position pos,
        in Direction dir,
        in Speed speed,
        ref Walkable walk,
        [Data] float deltaTime)
    {
        // Sem movimento
        if (speed.Value <= 0f || dir is { X: 0, Y: 0 })
            return;

        // Acumula
        walk.Accumulator += speed.Value * deltaTime;

        // Ainda não acumulou o suficiente
        if (walk.Accumulator < SimulationConfig.CellSize)
            return;

        // Consome o acumulador
        walk.Accumulator -= SimulationConfig.CellSize;

        // Cria intenção de movimento
        var intent = new MovementIntent
        {
            TargetPosition = new Position
            {
                X = pos.X + dir. X, 
                Y = pos.Y + dir. Y, 
                Z = pos.Z
            },
        };

        World.Add<MovementIntent>(entity, intent);
    }
    
    [Query]
    [All<MovementIntent, MapId>]
    [None<MovementApproved, MovementBlocked>]
    private void ValidateMovement(
        in Entity entity,
        in MapId mapId,
        in MovementIntent intent)
    {
        var result = mapIndex.ValidateMove(
            mapId.Value,
            intent.TargetPosition,
            entity
        );

        if (result == MovementResult.Allowed)
        {
            World.Add<MovementApproved>(entity);
        }
        else
        {
            LogWarning("[Movement] Blocked move for {Entity} to ({X},{Y},{Floor}) on map {MapId}: {Reason}",
                entity,
                intent.TargetPosition.X,
                intent.TargetPosition.Y,
                intent.TargetPosition.Z,
                mapId.Value,
                result);
            World.Add<MovementBlocked>(entity, new MovementBlocked { Reason = result });
        }
    }
    
    [Query]
    [All<MovementIntent, MovementApproved, Position>]
    private void ApplyApprovedMovement(
        in Entity entity,
        in MapId mapId,
        in MovementIntent intent,
        ref Position pos)
    {
        var spatial = mapIndex.GetMapSpatial(mapId.Value);
        if (!spatial.TryMove(pos, intent.TargetPosition, entity))
        {
            LogWarning(
                "[Movement] Post-approval move failed for {Entity} to ({X},{Y},{Floor}) on map {MapId}. Marking as blocked.",
                entity,
                intent.TargetPosition.X,
                intent.TargetPosition.Y,
                intent.TargetPosition.Z,
                mapId.Value);
            
            World.Remove<MovementApproved>(entity);
            World.Add<MovementBlocked>(entity, new MovementBlocked { Reason = MovementResult.BlockedByEntity });
            return;
        }
        
        // Dispara o evento de movimento
        InvokeMovementEvent(entity: entity, from: pos, to: intent.TargetPosition);
        
        // Aplica a nova posição
        pos.X = intent.TargetPosition.X;
        pos.Y = intent.TargetPosition.Y;
        pos.Z = intent.TargetPosition.Z;
        // Limpa os componentes temporários
        World.Remove<MovementIntent, MovementApproved>(entity);
    }

    [Query]
    [All<MovementIntent, MovementBlocked>]
    private void CleanupBlockedMovement(
        in Entity entity,
        in MovementBlocked blocked)
    {
        World.Remove<MovementIntent, MovementBlocked>(entity);
    }
    
    private void InvokeMovementEvent(in Entity entity, in Position from, in Position to)
    {
        var moveEvent = new MovementEvent(entity, from, to);
        eventBus.Send(ref moveEvent);
    }
}