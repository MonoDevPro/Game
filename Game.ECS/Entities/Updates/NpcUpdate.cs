using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Data;
using Game.ECS.Entities.Repositories;

namespace Game.ECS.Entities.Updates;

public static partial class EntityUpdates
{
    public static bool ApplyNpcState(this World world, Entity entity, NpcStateData data)
    {
        ref var position = ref world.Get<Position>(entity);
        ref var facing = ref world.Get<Facing>(entity);
        ref var velocity = ref world.Get<Velocity>(entity);
        position.X = data.PositionX;
        position.Y = data.PositionY;
        position.Z = data.PositionZ;
        velocity.DirectionX = data.VelocityX;
        velocity.DirectionY = data.VelocityY;
        velocity.Speed = data.Speed;
        facing.DirectionX = data.FacingX;
        facing.DirectionY = data.FacingY;
        return true;
    }
    
    public static bool ApplyNpcVitals(this World world, Entity entity, NpcHealthData data)
    {
        ref var health = ref world.Get<Health>(entity);
        health.Current = data.CurrentHp;
        health.Max = data.MaxHp;
        return true;
    }
}