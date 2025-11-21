using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Data;

namespace Game.ECS.Entities.Factories;

public static partial class EntityBuilder
{
    public static NPCData BuildNpcData(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
        ref var info = ref world.Get<Info>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var attackPower = ref world.Get<AttackPower>(entity);
        ref var defense = ref world.Get<Defense>(entity);

        return new NPCData
        {
            NetworkId = networkId.Value, MapId = mapId.Value,
            Gender = info.GenderId, Vocation = info.VocationId,
            PositionX = position.X, PositionY = position.Y, PositionZ = position.Z,
            Hp = health.Current, MaxHp = health.Max, HpRegen = health.RegenerationRate,
            PhysicalAttack = attackPower.Physical, MagicAttack = attackPower.Magical,
            PhysicalDefense = defense.Physical, MagicDefense = defense.Magical
        };
    }
    
    public static NpcStateData BuildNpcStateData(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var facing = ref world.Get<Facing>(entity);
        ref var velocity = ref world.Get<Velocity>(entity);
        ref var health = ref world.Get<Health>(entity);

        return new NpcStateData
        {
            NetworkId = networkId.Value,
            PositionX = position.X,
            PositionY = position.Y,
            PositionZ = position.Z,
            FacingX = facing.DirectionX,
            FacingY = facing.DirectionY,
            Speed = velocity.Speed,
        };
    }
    
    public static NpcHealthData BuildNpcHealthData(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var health = ref world.Get<Health>(entity);

        return new NpcHealthData
        {
            NetworkId = networkId.Value,
            CurrentHp = health.Current,
            MaxHp = health.Max
        };
    }
    
}