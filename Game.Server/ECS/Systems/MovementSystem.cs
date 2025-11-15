using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class MovementSystem(World world, IMapService mapService)
    : GameSystem(world)
{
    public MovementSystem(World world)
        : this(world, new MapService()) { }

    [Query]
    [All<PlayerControlled, Facing, Velocity, DirtyFlags>]
    [None<Dead>]
    private void ProcessEntityFacing(in Velocity velocity, ref Facing facing, ref DirtyFlags dirty, [Data] float _)
    {
        if (velocity is { DirectionX: 0, DirectionY: 0 }) return;
        int previousX = facing.DirectionX;
        int previousY = facing.DirectionY;

        facing.DirectionX = velocity.DirectionX;
        facing.DirectionY = velocity.DirectionY;

        if (previousX != facing.DirectionX || previousY != facing.DirectionY)
            dirty.MarkDirty(DirtyComponentType.Facing);
    }
    
    [Query]
    [All<Position, Movement, Velocity, Walkable, DirtyFlags, MapId>]
    [None<Dead>]
    private void ProcessMovement(in Entity e, ref Position pos, ref Movement movement, ref Velocity velocity, ref DirtyFlags dirty, in MapId mapId, [Data] float deltaTime)
    {
        if (velocity is { DirectionX: 0, DirectionY: 0 }) return;
        
        movement.Timer += velocity.Speed * deltaTime;
        var grid = mapService.GetMapGrid(mapId.Value);
        var spatial = mapService.GetMapSpatial(mapId.Value);

        var moveResult = MovementLogic.TryComputeStep(e, pos, velocity, movement, deltaTime, grid, spatial, out var candidatePos);
        
        if (moveResult != MovementLogic.MovementResult.Allowed && 
            moveResult != MovementLogic.MovementResult.None)
        {
            movement.Timer = 0f;
            return;
        }
        
        if (movement.Timer >= SimulationConfig.CellSize)
            movement.Timer -= SimulationConfig.CellSize;

        // movimento permitido ->
        if (!spatial.Update(pos, candidatePos, e))
        {
            spatial.Remove(pos, e);
            spatial.Insert(candidatePos, e);
        }
        pos = candidatePos;
        dirty.MarkDirty(DirtyComponentType.Position);
    }
    
    [Query]
    [All<Velocity>]
    private void DecayVelocity(ref Velocity velocity, [Data] float deltaTime)
    {
        // desacelera velocity gradualmente quando não há input
        if (velocity is { DirectionX: 0, DirectionY: 0 })
        {
            float decayRate = 10f; // unidades por segundo ao quadrado
            velocity.Speed -= decayRate * deltaTime;
            if (velocity.Speed < 0f)
                velocity.Speed = 0f;
        }
    }
}