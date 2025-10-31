using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace GodotClient.ECS.Systems;

public sealed partial class MovementSystem(
    World world,
    IMapService mapService)
    : GameSystem(world)
{
    public MovementSystem(World world)
        : this(world, new MapService()) { }

    [Query]
    [All<PlayerControlled, Facing, Velocity, DirtyFlags>]
    [None<Dead>]
    private void ProcessEntityFacing(in Entity e, in Velocity velocity, ref Facing facing, ref DirtyFlags dirty, [Data] float _)
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

        // compute result (pode passar entidade se preferir que a lógica compare occupant != mover)
        
        // chama a lógica pura
        var grid = mapService.GetMapGrid(mapId.Value);
        var spatial = mapService.GetMapSpatial(mapId.Value);
        if (MovementLogic.TryComputeStep(e, pos, velocity, movement, deltaTime, grid, spatial, out var candidatePos) != MovementLogic.MovementResult.Allowed)
        {
            // movimento bloqueado -> mantém velocity para contínuo input (mesma behavior)
            return;
        }

        // no servidor fazemos commit ao estado verdadeiro (side-effects)
        // Certifique-se que operações em spatial/world sejam feitas de forma atômica / thread-safe
        movement.Timer += velocity.Speed * deltaTime;
        if (movement.Timer >= SimulationConfig.CellSize)
        {
            movement.Timer -= SimulationConfig.CellSize;

            // atualiza spatial de forma segura
            if (!spatial.Update(pos, candidatePos, e))
            {
                spatial.Remove(pos, e);
                spatial.Insert(candidatePos, e);
            }

            // escreve posição no World (isso é side-effect)
            pos = candidatePos;
        }
    }

}