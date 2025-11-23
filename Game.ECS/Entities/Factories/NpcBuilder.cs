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
        ref var info = ref world.Get<NpcInfo>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var facing = ref world.Get<Facing>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        ref var attackPower = ref world.Get<AttackPower>(entity);
        ref var walkable = ref world.Get<Walkable>(entity);
        ref var attackable = ref world.Get<Attackable>(entity);
        ref var defense = ref world.Get<Defense>(entity);

        return new NPCData
        {
            NetworkId = networkId.Value, MapId = mapId.Value,
            Gender = info.GenderId, Vocation = info.VocationId,
            PositionX = position.X, PositionY = position.Y, Floor = floor.Level,
            FacingX = facing.DirectionX, FacingY = facing.DirectionY,
            Hp = health.Current, MaxHp = health.Max, HpRegen = health.RegenerationRate,
            Mp = mana.Current, MaxMp = mana.Max, MpRegen = mana.RegenerationRate,
            MovementSpeed = walkable.CurrentModifier, AttackSpeed = attackable.CurrentModifier,
            PhysicalAttack = attackPower.Physical, MagicAttack = attackPower.Magical,
            PhysicalDefense = defense.Physical, MagicDefense = defense.Magical
        };
    }
    
    public static NpcStateData BuildNpcStateData(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var facing = ref world.Get<Facing>(entity);
        ref var velocity = ref world.Get<Velocity>(entity);

        return new NpcStateData
        {
            NetworkId = networkId.Value,
            PositionX = position.X,
            PositionY = position.Y,
            Floor = floor.Level,
            VelocityX = velocity.X,
            VelocityY = velocity.Y,
            Speed = velocity.Speed,
            FacingX = facing.DirectionX,
            FacingY = facing.DirectionY,
        };
    }
    
    public static NpcVitalsData BuildNpcHealthData(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);

        return new NpcVitalsData
        {
            NetworkId = networkId.Value,
            CurrentHp = health.Current,
            MaxHp = health.Max,
            CurrentMp = mana.Current,
            MaxMp = mana.Max
        };
    }
    
}