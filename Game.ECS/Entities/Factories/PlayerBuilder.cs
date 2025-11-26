using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Data;

namespace Game.ECS.Entities.Factories;

public static partial class EntityBuilder
{
    public static PlayerData BuildPlayerDataSnapshot(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var playerId = ref world.Get<PlayerId>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
        ref var info = ref world.Get<PlayerInfo>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var facing = ref world.Get<Facing>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        ref var walkable = ref world.Get<Walkable>(entity);
        ref var combatStats = ref world.Get<CombatStats>(entity);

        return new PlayerData
        {
            Vocation = info.VocationId, Gender = info.GenderId,
            NetworkId = networkId.Value, PlayerId = playerId.Value, MapId = mapId.Value,
            PosX = position.X, PosY = position.Y, Floor = floor.Level,
            FacingX = facing.DirectionX, FacingY = facing.DirectionY,
            Hp = health.Current, MaxHp = health.Max, HpRegen = health.RegenerationRate,
            Mp = mana.Current, MaxMp = mana.Max, MpRegen = mana.RegenerationRate,
            MovementSpeed = walkable.CurrentModifier, AttackSpeed = combatStats.AttackSpeed,
            PhysicalAttack = combatStats.AttackPower, MagicAttack = combatStats.MagicPower,
            PhysicalDefense = combatStats.Defense, MagicDefense = combatStats.MagicDefense
        };
    }
    
    public static PlayerStateData BuildPlayerStateSnapshot(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var facing = ref world.Get<Facing>(entity);
        ref var velocity = ref world.Get<Velocity>(entity);
        
        return new PlayerStateData
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
    
    public static PlayerVitalsData BuildPlayerVitalsSnapshot(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);

        return new PlayerVitalsData
        {
            NetworkId = networkId.Value,
            CurrentHp = health.Current,
            MaxHp = health.Max,
            CurrentMp = mana.Current,
            MaxMp = mana.Max
        };
    }
}