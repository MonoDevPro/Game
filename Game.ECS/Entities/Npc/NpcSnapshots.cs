using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.ECS.Entities.Npc;

/// <summary>
/// Dados de um NPC (controlado por IA).
/// </summary>
public readonly record struct NpcSnapshot(int NetworkId, int MapId, string Name, int X, int Y, sbyte Floor, sbyte DirX, sbyte DirY, int Hp, int MaxHp, int Mp, int MaxMp);

public readonly record struct NpcStateSnapshot(int NetworkId, int X, int Y, sbyte Floor, float Speed, sbyte DirectionX, sbyte DirectionY);

public readonly record struct NpcVitalsSnapshot(int NetworkId, int CurrentHp, int MaxHp, int CurrentMp, int MaxMp);

public class NpcSnapshotBuilder(World world, GameResources resources)
{
    public NpcSnapshot BuildNpcData(Entity entity)
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
            Name = resources.Strings.Get(name.Value),
            X = position.X, Y = position.Y, Floor = floor.Level,
            DirX = direction.DirectionX, DirY = direction.DirectionY,
            Hp = health.Current, MaxHp = health.Max,
            Mp = mana.Current, MaxMp = mana.Max,
        };
    }
    
    public NpcStateSnapshot BuildNpcStateData(Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var direction = ref world.Get<Direction>(entity);
        ref var velocity = ref world.Get<Velocity>(entity);

        return new NpcStateSnapshot
        {
            NetworkId = networkId.Value,
            X = position.X,
            Y = position.Y,
            Floor = floor.Level,
            Speed = velocity.Speed,
            DirectionX = direction.DirectionX,
            DirectionY = direction.DirectionY,
        };
    }
    
    public NpcVitalsSnapshot BuildNpcVitalsSnapshot(Entity entity)
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