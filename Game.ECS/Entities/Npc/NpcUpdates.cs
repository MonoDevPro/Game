using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Entities.Npc;

public static class NpcUpdate
{
    public static void ApplyNpcState(this World world, Entity entity, NpcStateSnapshot snapshot)
    {
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var direction = ref world.Get<Direction>(entity);
        ref var velocity = ref world.Get<Speed>(entity);
        
        position.X = snapshot.X;
        position.Y = snapshot.Y;
        floor.Level = snapshot.Floor;
        direction.X = snapshot.DirectionX;
        direction.Y = snapshot.DirectionY;
        velocity.Value = snapshot.Speed;
    }
    
    public static void ApplyNpcVitals(this World world, Entity entity, NpcVitalsSnapshot snapshot)
    {
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        
        health.Current = snapshot.CurrentHp;
        health.Max = snapshot.MaxHp;
        mana.Current = snapshot.CurrentMp;
        mana.Max = snapshot.MaxMp;
    }
}