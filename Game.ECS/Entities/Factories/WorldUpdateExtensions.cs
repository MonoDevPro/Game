using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;

namespace Game.ECS.Entities.Factories;

/// <summary>
/// Extension methods for applying state updates to entities in the ECS World.
/// </summary>
public static class WorldUpdateExtensions
{
    /// <summary>
    /// Applies a player state snapshot to the entity identified by networkId.
    /// </summary>
    public static bool ApplyPlayerState(this World world, IPlayerIndex index, PlayerStateSnapshot snapshot)
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
    
    /// <summary>
    /// Applies a player vitals snapshot to the entity identified by networkId.
    /// </summary>
    public static bool ApplyPlayerVitals(this World world, IPlayerIndex index, PlayerVitalsSnapshot snapshot)
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
    
    /// <summary>
    /// Applies an NPC state snapshot to an entity.
    /// </summary>
    public static bool ApplyNpcState(this World world, Entity entity, NpcStateSnapshot snapshot)
    {
        if (!world.IsAlive(entity))
            return false;
            
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
    
    /// <summary>
    /// Applies an NPC vitals snapshot to an entity.
    /// </summary>
    public static bool ApplyNpcVitals(this World world, Entity entity, NpcVitalsSnapshot snapshot)
    {
        if (!world.IsAlive(entity))
            return false;
            
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        
        health.Current = snapshot.CurrentHp;
        health.Max = snapshot.MaxHp;
        mana.Current = snapshot.CurrentMp;
        mana.Max = snapshot.MaxMp;
        return true;
    }
}
