using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Schema.Components;
using Game.ECS.Services;
using Game.ECS.Services.Index;

namespace Game.ECS.Entities.Player;

public static class PlayerBuilders
{
    public static PlayerSnapshot BuildPlayerSnapshot(this World world, Entity entity, ResourceIndex<string> namesResource)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var playerId = ref world.Get<UniqueID>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
        ref var name = ref world.Get<NameHandle>(entity);
        ref var gender = ref world.Get<GenderId>(entity);
        ref var vocation = ref world.Get<VocationId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var facing = ref world.Get<Direction>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        ref var walkable = ref world.Get<Walkable>(entity);
        ref var combatStats = ref world.Get<CombatStats>(entity);
        
        return new PlayerSnapshot(
            PlayerId: playerId.Value,
            NetworkId: networkId.Value,
            MapId: mapId.Value,
            Name: namesResource.Get(name.Value),
            GenderId: gender.Value,
            VocationId: vocation.Value,
            PosX: position.X,
            PosY: position.Y,
            Floor: floor.Value,
            DirX: facing.X,
            DirY: facing.Y,
            Hp: health.Current,
            MaxHp: health.Max,
            Mp: mana.Current,
            MaxMp: mana.Max,
            MovementSpeed: walkable.CurrentModifier,
            AttackSpeed: combatStats.AttackSpeed,
            PhysicalAttack: combatStats.AttackPower,
            MagicAttack: combatStats.MagicPower,
            PhysicalDefense: combatStats.Defense,
            MagicDefense: combatStats.MagicDefense
        );
    }
    
    public static StateSnapshot BuildPlayerStateSnapshot(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var walkable = ref world.Get<Walkable>(entity);
        ref var direction = ref world.Get<Direction>(entity);
        
        return new StateSnapshot
        {
            NetworkId = networkId.Value,
            PosX = position.X,
            PosY = position.Y,
            Floor = floor.Value,
            Speed = walkable.BaseSpeed * walkable.CurrentModifier,
            DirX = direction.X,
            DirY = direction.Y,
        };
    }
    
    public static VitalsSnapshot BuildPlayerVitalsSnapshot(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);

        return new VitalsSnapshot
        {
            NetworkId = networkId.Value,
            Hp = health.Current,
            MaxHp = health.Max,
            Mp = mana.Current,
            MaxMp = mana.Max
        };
    }
}