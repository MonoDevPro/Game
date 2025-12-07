using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Entities.Components;
using Game.ECS.Events;
using Game.ECS.Schema.Components;
using Game.ECS.Services.Map;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

public sealed partial class MovementSystem : GameSystem
{
    private readonly IMapIndex _mapIndex;
    private readonly GameEventBus _eventBus;

    public MovementSystem(World world, IMapIndex mapIndex, GameEventBus eventBus, ILogger<MovementSystem>? logger = null)
        : base(world, logger)
    {
        _mapIndex = mapIndex;
        _eventBus = eventBus;
    }
    [Query]
    [All<Position, Direction, Speed, Walkable, Floor>]
    [None<Dead, MovementIntent>] // Não processa se já tem intent pendente
    private void GenerateIntent(
        in Entity entity,
        in Position pos,
        in Direction dir,
        in Speed speed,
        in Floor floor,
        ref Walkable walk,
        [Data] float deltaTime)
    {
        // Sem movimento
        if (speed. Value <= 0f || dir is { X: 0, Y: 0 })
            return;

        // Acumula
        walk.Accumulator += speed.Value * deltaTime;

        // Ainda não acumulou o suficiente
        if (walk. Accumulator < SimulationConfig.CellSize)
            return;

        // Consome o acumulador
        walk.Accumulator -= SimulationConfig.CellSize;

        // Cria intenção de movimento
        var intent = new MovementIntent
        {
            TargetPosition = new Position { X = pos.X + dir. X, Y = pos.Y + dir. Y }, 
            TargetFloor = floor.Value
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
        var result = _mapIndex.ValidateMove(
            mapId. Value,
            intent.TargetPosition,
            intent. TargetFloor,
            entity
        );

        if (result == MovementResult. Allowed)
            World.Add<MovementApproved>(entity);
        else
        {
            LogWarning(
                "[Movement] Blocked move for {Entity} to ({X},{Y},{Floor}) on map {MapId}: {Reason}",
                entity,
                intent.TargetPosition.X,
                intent.TargetPosition.Y,
                intent.TargetFloor,
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
        ref Floor floor,
        in MovementIntent intent,
        ref Position pos)
    {
        var moveEvent = new MovementEvent(entity, pos, intent.TargetPosition);
        _eventBus.Send(ref moveEvent);

        var spatial = _mapIndex.GetMapSpatial(mapId.Value);
        if (!spatial.TryMove(pos, floor.Value, intent.TargetPosition, intent.TargetFloor, entity))
        {
            LogWarning(
                "[Movement] Post-approval move failed for {Entity} to ({X},{Y},{Floor}) on map {MapId}. Marking as blocked.",
                entity,
                intent.TargetPosition.X,
                intent.TargetPosition.Y,
                intent.TargetFloor,
                mapId.Value);
            World.Remove<MovementApproved>(entity);
            World.Add<MovementBlocked>(entity, new MovementBlocked { Reason = MovementResult.BlockedByEntity });
            return;
        }
        
        // Aplica a nova posição
        pos.X = intent.TargetPosition.X;
        pos.Y = intent.TargetPosition.Y;
        floor.Value = intent.TargetFloor;
        // Limpa os componentes temporários
        World.Remove<MovementIntent, MovementApproved>(entity);
    }

    [Query]
    [All<MovementIntent, MovementBlocked>]
    private void CleanupBlockedMovement(
        in Entity entity,
        in MovementBlocked blocked)
    {
        // Limpa os componentes temporários
        World.Remove<MovementIntent, MovementBlocked>(entity);
    }
    
    public static (sbyte X, sbyte Y) GetDirectionTowards(in Position from, in Position to) 
        => ((sbyte)Math.Sign(to.X - from.X), (sbyte)Math.Sign(to.Y - from.Y));
    
    public static float CalculateDistance(in Position a, in Position b)
    {
        float deltaX = b.X - a.X;
        float deltaY = b.Y - a.Y;
        return MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
    
    public static int ManhattanDistance(Position pos, Position other) => Math.Abs(pos.X - other.X) + Math.Abs(pos.Y - other.Y);
    
    public static int EuclideanDistanceSquared(Position pos, Position other) => (pos.X - other.X) * (pos.X - other.X) + (pos.Y - other.Y) * (pos.Y - other.Y);
}