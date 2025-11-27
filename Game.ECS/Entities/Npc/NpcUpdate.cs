using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Entities.Npc;

public class NpcUpdate(World world)
{
    public bool ApplyNpcState(Entity entity, NpcStateSnapshot snapshot)
    {
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var direction = ref world.Get<Direction>(entity);
        ref var velocity = ref world.Get<Velocity>(entity);
        
        position = new Position(X: snapshot.X, Y: snapshot.Y);
        floor.Level = snapshot.Floor;
        velocity.Speed = snapshot.Speed;
        direction.DirectionX = snapshot.DirectionX;
        direction.DirectionY = snapshot.DirectionY;
        return true;
    }
    
    public bool ApplyNpcVitals(Entity entity, NpcVitalsSnapshot snapshot)
    {
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        
        health.Current = snapshot.CurrentHp;
        health.Max = snapshot.MaxHp;
        mana.Current = snapshot.CurrentMp;
        mana.Max = snapshot.MaxMp;
        return true;
    }
}