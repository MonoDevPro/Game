using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.ECS.Entities.Npc;

public static class NpcBuilder
{
    public static NpcSnapshot BuildNpcSnapshot(this World world, Entity entity, ResourceStack<string> resources)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
        ref var name = ref world.Get<NameHandle>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var direction = ref world.Get<Direction>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);

        return new NpcSnapshot
        {
            NetworkId = networkId.Value, MapId = mapId.Value,
            Name = resources.Get(name.Value),
            X = position.X, Y = position.Y, Floor = floor.Level,
            DirX = direction.X, DirY = direction.Y,
            Hp = health.Current, MaxHp = health.Max,
            Mp = mana.Current, MaxMp = mana.Max,
        };
    }
    
    public static NpcStateSnapshot BuildNpcStateSnapshot(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var direction = ref world.Get<Direction>(entity);
        ref var velocity = ref world.Get<Speed>(entity);

        return new NpcStateSnapshot
        {
            NetworkId = networkId.Value,
            X = position.X,
            Y = position.Y,
            Floor = floor.Level,
            Speed = velocity.Value,
            DirectionX = direction.X,
            DirectionY = direction.Y,
        };
    }
    
    public static NpcVitalsSnapshot BuildNpcVitalsSnapshot(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);

        return new NpcVitalsSnapshot
        {
            NetworkId = networkId.Value,
            CurrentHp = health.Current,
            MaxHp = health.Max,
            CurrentMp = mana.Current,
            MaxMp = mana.Max
        };
    }
}