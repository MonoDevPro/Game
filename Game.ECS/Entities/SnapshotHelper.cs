using Arch.Core;
using Game.DTOs.Game.Npc;
using Game.DTOs.Game.Player;
using Game.ECS.Entities.Components;
using Game.ECS.Schema.Components;

namespace Game.ECS.Entities;

public static class SnapshotHelper
{
    public static PositionStateData BuildState(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var walkable = ref world.Get<Walkable>(entity);
        ref var direction = ref world.Get<Direction>(entity);

        return new PositionStateData
        {
            NetworkId = networkId.Value,
            X = position.X,
            Y = position.Y,
            Floor = floor.Value,
            DirX = direction.X,
            DirY = direction.Y,
        };
    }

    public static VitalsData BuildVitals(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);

        return new VitalsData
        {
            NetworkId = networkId.Value,
            CurrentHp = health.Current,
            MaxHp = health.Max,
            CurrentMp = mana.Current,
            MaxMp = mana.Max
        };
    }
    
    public static PlayerData BuildPlayerSnapshot(this World world, Entity entity, string name)
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

        return new PlayerData(
            PlayerId: playerId.Value,
            NetworkId: networkId.Value,
            MapId: mapId.Value,
            Name: name,
            Gender: gender.Value,
            Vocation: vocation.Value,
            X: position.X,
            Y: position.Y,
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
    
    public static NpcData BuildNpcSnapshot(this World world, Entity entity, string name)
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

        return new NpcData(
            NpcId: npcId.Value,
            NetworkId: networkId.Value,
            MapId: mapId.Value,
            Name: name,
            Gender: gender.Value,
            Vocation: vocation.Value,
            X: position.X,
            Y: position.Y,
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
