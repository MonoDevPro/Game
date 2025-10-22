using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Entities;
using Game.ECS.Entities.Factories;
using Game.ECS.Services;
using Game.ECS.Utils;

namespace Game.ECS.Systems;

public sealed partial class MovementSystem : GameSystem
{
    private readonly IMapService _mapService;

    public MovementSystem(World world, GameEventSystem events, EntityFactory factory)
        : this(world, events, factory, new MapService())
    {
    }

    public MovementSystem(World world, GameEventSystem events, EntityFactory factory, IMapService mapService)
        : base(world, events, factory)
    {
        _mapService = mapService;
    }

    [Query]
    [All<PlayerControlled, Facing, Velocity, DirtyFlags>]
    [None<Dead>]
    private void ProcessEntityFacing(in Entity e, in Velocity velocity, ref Facing facing, ref DirtyFlags dirty, [Data] float _)
    {
        if (velocity.DirectionX == 0 && velocity.DirectionY == 0) return;
        int previousX = facing.DirectionX;
        int previousY = facing.DirectionY;

        facing.DirectionX = velocity.DirectionX;
        facing.DirectionY = velocity.DirectionY;

        if (previousX != facing.DirectionX || previousY != facing.DirectionY)
        {
            dirty.MarkDirty(DirtyComponentType.Facing);
            Events.RaiseFacingChanged(e, facing.DirectionX, facing.DirectionY);
        }
    }

    [Query]
    [All<Position, Movement, Velocity, Walkable, DirtyFlags, MapId>]
    [None<Dead>]
    private void ProcessMovement(in Entity e, ref Position pos, ref Movement movement, ref Velocity velocity, ref DirtyFlags dirty, in MapId mapId, [Data] float deltaTime)
    {
        if (TryStep(e, ref pos, ref movement, ref velocity, in mapId, deltaTime))
        {
            dirty.MarkDirty(DirtyComponentType.Position);
            Events.RaisePositionChanged(e, pos.X, pos.Y);
        }
    }

    private bool TryStep(in Entity entity, ref Position pos, ref Movement movement, ref Velocity vel, in MapId mapId, float dt)
    {
        if ((vel.DirectionX == 0 && vel.DirectionY == 0) || vel.Speed <= 0f)
            return false;

        movement.Timer += vel.Speed * dt;
        if (movement.Timer < SimulationConfig.CellSize)
            return false;

        var newPos = new Position
        {
            X = pos.X + vel.DirectionX,
            Y = pos.Y + vel.DirectionY,
            Z = pos.Z
        };

        var mapGrid = _mapService.GetMapGrid(mapId.Value);
        if (!mapGrid.InBounds(newPos))
        {
            vel.Speed = 0f;
            return false;
        }

        if (mapGrid.IsBlocked(newPos))
        {
            vel.Speed = 0f;
            return false;
        }

        var mapSpatial = _mapService.GetMapSpatial(mapId.Value);
        if (mapSpatial.TryGetFirstAt(newPos, out var occupant) && occupant != entity)
        {
            vel.Speed = 0f;
            return false;
        }

        movement.Timer -= SimulationConfig.CellSize;

        if (!mapSpatial.Update(pos, newPos, entity))
        {
            mapSpatial.Remove(pos, entity);
            mapSpatial.Insert(newPos, entity);
        }

        pos = newPos;
        vel.Speed = 0f;
        return true;
    }
}