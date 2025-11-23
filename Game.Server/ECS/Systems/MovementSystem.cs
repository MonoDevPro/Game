using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class MovementSystem(World world, IMapService mapService) : GameSystem(world)
{
    [Query]
    [All<Facing, Velocity, DirtyFlags>]
    [None<Dead>]
    private void ProcessEntityFacing(in Velocity velocity, ref Facing facing, ref DirtyFlags dirty)
    {
        if (velocity is { X: 0, Y: 0 }) return;
        int previousX = facing.DirectionX;
        int previousY = facing.DirectionY;

        facing.DirectionX = velocity.X;
        facing.DirectionY = velocity.Y;

        if (previousX != facing.DirectionX || previousY != facing.DirectionY)
            dirty.MarkDirty(DirtyComponentType.State);
    }
    
    [Query]
    [All<Position, Movement, Velocity, Walkable, DirtyFlags, MapId>]
    [None<Dead>]
    private void ProcessMovement(in Entity e, 
        in MapId mapId, 
        in Position pos, 
        in Floor floor,
        in Velocity velocity,
        ref Movement movement, 
        ref DirtyFlags dirty, 
        [Data] float deltaTime)
    {
        if (velocity is { X: 0, Y: 0 }) return;
    
        movement.Timer += velocity.Speed * deltaTime;
        
        var grid = mapService.GetMapGrid(mapId.Value);
        var spatial = mapService.GetMapSpatial(mapId.Value);

        var moveResult = MovementLogic.TryComputeStep(
            e, pos, floor, velocity, movement, deltaTime, grid, spatial, out var candidatePos);
    
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
    [All<Velocity>]
    private void DecayVelocity(ref Velocity velocity, [Data] float deltaTime)
    {
        // desacelera velocity gradualmente quando não há input
        if (velocity is { X: 0, Y: 0 })
        {
            float decayRate = 10f; // unidades por segundo ao quadrado
            velocity.Speed -= decayRate * deltaTime;
            if (velocity.Speed < 0f)
                velocity.Speed = 0f;
        }
    }
}