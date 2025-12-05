using Arch.Core;
using Game.ECS.Schema.Components;
using Game.ECS.Schema.Snapshots;

namespace Game.ECS.Entities;

public static class SnapshotHelper
{
    public static StateSnapshot BuildState(this World world, Entity entity)
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

    public static VitalsSnapshot BuildVitals(this World world, Entity entity)
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
    
    public static PlayerSnapshot BuildPlayerSnapshot(this World world, Entity entity, string name)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var playerId = ref world.Get<UniqueID>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
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
            Name: name,
            GenderId: gender.Value,
            VocationId: vocation.Value,
            PosX: position.X,
            PosY: position.Y,
            Floor: floor.Value,
            DirX: facing.X,
            DirY: facing.Y,
            Hp: health.Current,
            MaxHp: health.Max,
            HpRegen: health.RegenerationRate,
            Mp: mana.Current,
            MaxMp: mana.Max,
            MpRegen: mana.RegenerationRate,
            MovementSpeed: walkable.CurrentModifier,
            AttackSpeed: combatStats.AttackSpeed,
            PhysicalAttack: combatStats.AttackPower,
            MagicAttack: combatStats.MagicPower,
            PhysicalDefense: combatStats.Defense,
            MagicDefense: combatStats.MagicDefense
        );
    }
    
    public static NpcSnapshot BuildNpcSnapshot(this World world, Entity entity, string name)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var npcId = ref world.Get<UniqueID>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
        ref var gender = ref world.Get<GenderId>(entity);
        ref var vocation = ref world.Get<VocationId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var facing = ref world.Get<Direction>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        ref var walkable = ref world.Get<Walkable>(entity);
        ref var combatStats = ref world.Get<CombatStats>(entity);

        return new NpcSnapshot(
            NpcId: npcId.Value,
            NetworkId: networkId.Value,
            MapId: mapId.Value,
            Name: name,
            GenderId: gender.Value,
            VocationId: vocation.Value,
            PosX: position.X,
            PosY: position.Y,
            Floor: floor.Value,
            DirX: facing.X,
            DirY: facing.Y,
            Hp: health.Current,
            MaxHp: health.Max,
            HpRegen: health.RegenerationRate,
            Mp: mana.Current,
            MaxMp: mana.Max,
            MpRegen: mana.RegenerationRate,
            MovementSpeed: walkable.CurrentModifier,
            AttackSpeed: combatStats.AttackSpeed,
            PhysicalAttack: combatStats.AttackPower,
            MagicAttack: combatStats.MagicPower,
            PhysicalDefense: combatStats.Defense,
            MagicDefense: combatStats.MagicDefense
        );
    }
}
