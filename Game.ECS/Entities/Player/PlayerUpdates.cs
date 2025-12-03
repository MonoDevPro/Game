using Arch.Core;
using Game.ECS.Schema.Components;
using Game.ECS.Services;

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
    
    /// <summary>
    /// Aplica estado a partir de EntityIndex por networkId.
    /// </summary>
    public static bool ApplyPlayerState(this World world, EntityIndex<int> index, StateSnapshot snapshot)
    {
        if (!index.TryGetEntity(snapshot.NetworkId, out var entity))
            return false;
        world.ApplyPlayerState(entity, snapshot);
        return true;
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
    
    /// <summary>
    /// Aplica vitals a partir de EntityIndex por networkId.
    /// </summary>
    public static bool ApplyPlayerVitals(this World world, EntityIndex<int> index, VitalsSnapshot snapshot)
    {
        if (!index.TryGetEntity(snapshot.NetworkId, out var entity))
            return false;
        world.ApplyPlayerVitals(entity, snapshot);
        return true;
    }
}