using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace GodotClient.ECS.Systems;

public sealed partial class LocalMovementSystem(World world, IMapService? mapService) 
    : GameSystem(world)
{
    [Query]
    [All<LocalPlayerTag, PlayerControlled, Velocity>]
    [None<Dead>]
    private void UpdateVelocity(in Entity e,
        ref Velocity velocity, in Walkable speed, ref PlayerInput input, [Data] in float _)
    {
        var (normalizedX, normalizedY) = MovementLogic.NormalizeInput(input.InputX, input.InputY);
        velocity.DirectionX = normalizedX;
        velocity.DirectionY = normalizedY;
        
        if (velocity is not { DirectionX: 0, DirectionY: 0 })
            velocity.Speed = MovementLogic.ComputeCellsPerSecond(in speed, input.Flags);
    }
    
    [Query]
    [All<LocalPlayerTag, Position, Movement, Velocity, Walkable, MapId>]
    [None<Dead>]
    private void ProcessMovement(in Entity e, ref Position pos, ref Movement movement, ref Velocity velocity, in MapId mapId, [Data] float deltaTime)
    {
        if (velocity is { DirectionX: 0, DirectionY: 0 }) return;
        
        movement.Timer += velocity.Speed * deltaTime;
        var grid = mapService?.GetMapGrid(mapId.Value);
        var spatial = mapService?.GetMapSpatial(mapId.Value);
        
        if (MovementLogic.TryComputeStep(e, pos, velocity, movement, deltaTime, grid, spatial, out var candidatePos) 
            != MovementLogic.MovementResult.Allowed)
            return;
        
        if (movement.Timer >= SimulationConfig.CellSize)
            movement.Timer -= SimulationConfig.CellSize;

        // movimento permitido ->
        if (spatial is not null && !spatial.Update(pos, candidatePos, e))
        {
            spatial.Remove(pos, e);
            spatial.Insert(candidatePos, e);
        }
        pos = candidatePos;
    }
}