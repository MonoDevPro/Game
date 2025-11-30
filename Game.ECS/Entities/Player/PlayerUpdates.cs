using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Entities.Player;

public static class EntityBuilder
{
    public static void ApplyPlayerState(this World world, Entity entity, StateSnapshot snapshot)
    {
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var facing = ref world.Get<Direction>(entity);
        ref var velocity = ref world.Get<Speed>(entity);
        position.X = snapshot.PosX;
        position.Y = snapshot.PosY;
        floor.Value = snapshot.Floor;
        velocity.Value = snapshot.Speed;
        facing.X = snapshot.DirX;
        facing.Y = snapshot.DirY;
    }
    
    public static void ApplyPlayerVitals(this World world, Entity entity, VitalsSnapshot snapshot)
    {
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        health.Current = snapshot.Hp;
        health.Max = snapshot.MaxHp;
        mana.Current = snapshot.Mp;
        mana.Max = snapshot.MaxMp;
    }
}