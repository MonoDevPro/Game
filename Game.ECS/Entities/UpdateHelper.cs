using Arch.Core;
using Game.DTOs.Game.Player;
using Game.ECS.Components;

namespace Game.ECS.Entities;

public static class UpdateHelper
{
    public static void UpdateState(this World world, Entity entity, ref StateData data)
    {
        ref var position = ref world.Get<Position>(entity);
        ref var facing = ref world.Get<Direction>(entity);
        ref var speed = ref world.Get<Speed>(entity);
        position.X = data.X;
        position.Y = data.Y;
        position.Z = data.Z;
        facing.X = data.DirX;
        facing.Y = data.DirY;
        speed.Value = data.Speed;
    }
    
    public static void UpdateVitals(this World world, Entity entity, ref VitalsData data)
    {
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        health.Current = data.CurrentHp;
        health.Max = data.MaxHp;
        mana.Current = data.CurrentMp;
        mana.Max = data.MaxMp;
    }
}