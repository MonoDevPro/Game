using Arch.Core;
using Game.DTOs.Player;
using Game.ECS.Components;

namespace Game.ECS.Entities;

public static class UpdateHelper
{
    public static void UpdateState(this World world, Entity entity, ref StateSnapshot snapshot)
    {
        ref var position = ref world.Get<Position>(entity);
        ref var facing = ref world.Get<Direction>(entity);
        position.X = snapshot.X;
        position.Y = snapshot.Y;
        position.Z = snapshot.Z;
        facing.X = snapshot.DirX;
        facing.Y = snapshot.DirY;
    }
    
    public static void UpdateVitals(this World world, Entity entity, ref VitalsSnapshot snapshot)
    {
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        health.Current = snapshot.CurrentHp;
        health.Max = snapshot.MaxHp;
        mana.Current = snapshot.CurrentMp;
        mana.Max = snapshot.MaxMp;
    }
}