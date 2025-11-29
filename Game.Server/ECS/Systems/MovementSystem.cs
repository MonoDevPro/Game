using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities;
using Game.ECS.Entities.Factories;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class MovementSystem(World world, IMapService mapService) : GameSystem(world, mapService)
{
    [Query]
    [All<Direction, Speed, DirtyFlags>]
    [None<Dead>]
    private void ProcessEntityFacing(in Speed speed, ref Direction direction, ref DirtyFlags dirty)
    {
        if (speed is { X: 0, Y: 0 }) return;
        int previousX = direction.X;
        int previousY = direction.Y;

        direction.X = speed.X;
        direction.Y = speed.Y;

        if (previousX != direction.X || previousY != direction.Y)
            dirty.MarkDirty(DirtyComponentType.State);
    }
    
    [Query]
    [All<Position, Movement, Speed, Walkable, DirtyFlags, MapId>]
    [None<Dead>]
    private void ProcessMovement(in Entity e, 
        in MapId mapId, 
        in Position pos, 
        in Floor floor,
        in Speed speed,
        ref Movement movement, 
        ref DirtyFlags dirty, 
        [Data] float deltaTime)
    {
        if (speed is { X: 0, Y: 0 }) return;
    
        movement.Timer += speed.Value * deltaTime;
        
        var grid = mapService.GetMapGrid(mapId.Value);
        var spatial = mapService.GetMapSpatial(mapId.Value);

        var moveResult = MovementLogic.TryComputeStep(
            e, pos, floor, speed, movement, deltaTime, grid, spatial, out var candidatePos);
    
        if (moveResult != MovementLogic.MovementResult.Allowed && 
            moveResult != MovementLogic.MovementResult.None)
        {
            movement.Timer = 0f;
            return;
        }
    
        if (movement.Timer >= SimulationConfig.CellSize)
            movement.Timer -= SimulationConfig.CellSize;

        dirty.MarkDirty(DirtyComponentType.State);
        
        // ✅ Apenas atualiza a position - o SpatialSyncSystem cuida do resto
        // ✅ Usa a extensão para marcar mudança
        World.SetPosition(e, candidatePos.ToPosition());
    }
    
    [Query]
    [All<Speed>]
    private void DecayVelocity(ref Speed speed, [Data] float deltaTime)
    {
        // desacelera velocity gradualmente quando não há input
        if (speed is { X: 0, Y: 0 })
        {
            float decayRate = 10f; // unidades por segundo ao quadrado
            speed.Value -= decayRate * deltaTime;
            if (speed.Value < 0f)
                speed.Value = 0f;
        }
    }
}