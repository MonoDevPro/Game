using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;
using Game.ECS.Entities.Repositories;

namespace Game.ECS.Entities.Updates;

public static class PlayerSetters
{
    public static bool ApplyPlayerState(this World world, PlayerIndex index, PlayerStateData data)
    {
        if (!index.TryGetEntity(data.NetworkId, out var entity))
            return false;
        ref var position = ref world.Get<Position>(entity);
        ref var facing = ref world.Get<Facing>(entity);
        position.X = data.PositionX;
        position.Y = data.PositionY;
        position.Z = data.PositionZ;
        facing.DirectionX = data.FacingX;
        facing.DirectionY = data.FacingY;
        return true;
    }
    
    public static bool ApplyPlayerVitals(this World world, PlayerIndex index, PlayerVitalsData data)
    {
        if (!index.TryGetEntity(data.NetworkId, out var entity))
            return false;
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        health.Current = data.CurrentHp;
        health.Max = data.MaxHp;
        mana.Current = data.CurrentMp;
        mana.Max = data.MaxMp;
        return true;
    }
}