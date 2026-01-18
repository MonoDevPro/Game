using Arch.Core;
using Game.DTOs.Player;
using Game.ECS.Components;
using Game.ECS.Services.Snapshot.Data;
using MemoryPack;

namespace Game.ECS.Services.Snapshot;

public static class PlayerSnapshotService
{
    public static PlayerData BuildPlayerSnapshot(this World world, Entity entity, string name)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var playerId = ref world.Get<UniqueID>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
        ref var gender = ref world.Get<GenderId>(entity);
        ref var vocation = ref world.Get<VocationId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var facing = ref world.Get<Direction>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
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
            Z: position.Z,
            DirX: facing.X,
            DirY: facing.Y,
            Hp: health.Current,
            MaxHp: health.Max,
            HpRegen: health.RegenerationRate,
            Mp: mana.Current,
            MaxMp: mana.Max,
            MpRegen: mana.RegenerationRate,
            PhysicalAttack: combatStats.AttackPower,
            MagicAttack: combatStats.MagicPower,
            PhysicalDefense: combatStats.Defense,
            MagicDefense: combatStats.MagicDefense
        );
    }
    
    public static PlayerVitalSnapshot BuildPlayerVitalSnapshot(this World world, Entity entity)
    {
        ref var playerId = ref world.Get<UniqueID>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);

        return new PlayerVitalSnapshot(
            playerId.Value,
            CurrentHp: health.Current,
            MaxHp: health.Max,
            CurrentMp: mana.Current,
            MaxMp: mana.Max
        );
    }
    
    public static void ApplyPlayerVitalSnapshot(this World world, Entity entity, PlayerVitalSnapshot snapshot)
    {
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);

        health.Current = snapshot.CurrentHp;
        health.Max = snapshot.MaxHp;

        mana.Current = snapshot.CurrentMp;
        mana.Max = snapshot.MaxMp;
    }
    
    
}