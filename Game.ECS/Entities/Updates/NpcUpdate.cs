using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Data;

namespace Game.ECS.Entities.Updates;

public static partial class EntityUpdates
{
    public static bool ApplyNpcState(this World world, Entity entity, NpcStateData data)
    {
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var facing = ref world.Get<Facing>(entity);
        ref var velocity = ref world.Get<Velocity>(entity);
        position = new Position(X: data.PositionX, Y: data.PositionY);
        floor.Level = data.Floor;
        velocity.X = data.VelocityX;
        velocity.Y = data.VelocityY;
        velocity.Speed = data.Speed;
        facing.DirectionX = data.FacingX;
        facing.DirectionY = data.FacingY;
        return true;
    }
    
    public static bool ApplyNpcVitals(this World world, Entity entity, NpcVitalsData data)
    {
        ref var health = ref world.Get<Health>(entity);
        health.Current = data.CurrentHp;
        health.Max = data.MaxHp;
        return true;
    }
}