using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Entities.Player;

public class PlayerUpdate(World world)
{
    public bool ApplyPlayerState(PlayerIndex index, PlayerStateSnapshot snapshot)
    {
        if (!index.TryGetEntity(snapshot.NetworkId, out var entity))
            return false;
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var facing = ref world.Get<Direction>(entity);
        ref var velocity = ref world.Get<Velocity>(entity);
        
        position = new Position(X: snapshot.PositionX, Y: snapshot.PositionY);
        floor.Level = snapshot.Floor;
        velocity.Speed = snapshot.Speed;
        facing.DirectionX = snapshot.DirX;
        facing.DirectionY = snapshot.DirY;
        return true;
    }
    
    public bool ApplyPlayerVitals(PlayerIndex index, PlayerVitalsSnapshot snapshot)
    {
        if (!index.TryGetEntity(snapshot.NetworkId, out var entity))
            return false;
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        
        health.Current = snapshot.Hp;
        health.Max = snapshot.MaxHp;
        mana.Current = snapshot.Mp;
        mana.Max = snapshot.MaxMp;
        return true;
    }
}