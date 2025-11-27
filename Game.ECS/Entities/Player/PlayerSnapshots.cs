using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.ECS.Entities.Player;

/// <summary>
/// Dados completos de um jogador (usado no spawn).
/// </summary>
public record PlayerSnapshot(int PlayerId, int NetworkId, int MapId, string Name, byte GenderId, byte VocationId, 
    int PosX, int PosY, sbyte Floor, sbyte DirX, sbyte DirY, int Hp, int MaxHp, int Mp, int MaxMp, float MovementSpeed, 
    float AttackSpeed, int PhysicalAttack, int MagicAttack, int PhysicalDefense, int MagicDefense);

/// <summary>
/// Estado atual de um jogador (posição, velocidade, direção).
/// </summary>
public readonly record struct PlayerStateSnapshot(int NetworkId, int PositionX, int PositionY, sbyte Floor, float Speed, sbyte DirX, sbyte DirY);

/// <summary>
/// Vitals atuais de um jogador (HP e MP).
/// </summary>
public readonly record struct PlayerVitalsSnapshot(int NetworkId, int Hp, int MaxHp, int Mp, int MaxMp);

public sealed class PlayerSnapshotBuilder(World world, GameResources resources)
{
    public PlayerSnapshot BuildPlayerSnapshot(Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var playerId = ref world.Get<PlayerId>(entity);
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
            Name: resources.Strings.Get(name.Value),
            GenderId: gender.Value,
            VocationId: vocation.Value,
            PosX: position.X,
            PosY: position.Y,
            Floor: floor.Level,
            DirX: facing.DirectionX,
            DirY: facing.DirectionY,
            Hp: health.Current,
            MaxHp: health.Max,
            Mp: mana.Current,
            MaxMp: mana.Max,
            MovementSpeed: walkable.CurrentModifier,
            AttackSpeed: combatStats.AttackSpeed,
            PhysicalAttack: combatStats.AttackPower,
            MagicAttack: combatStats.MagicPower,
            PhysicalDefense: combatStats.Defense,
            MagicDefense: combatStats.MagicDefense);
    }

    public PlayerStateSnapshot BuildPlayerStateSnapshot(Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var walkable = ref world.Get<Walkable>(entity);
        ref var direction = ref world.Get<Direction>(entity);

        return new PlayerStateSnapshot
        {
            NetworkId = networkId.Value,
            PositionX = position.X,
            PositionY = position.Y,
            Floor = floor.Level,
            Speed = walkable.BaseSpeed * walkable.CurrentModifier,
            DirX = direction.DirectionX,
            DirY = direction.DirectionY,
        };
    }

    public PlayerVitalsSnapshot BuildPlayerVitalsSnapshot(Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);

        return new PlayerVitalsSnapshot
        {
            NetworkId = networkId.Value,
            Hp = health.Current,
            MaxHp = health.Max,
            Mp = mana.Current,
            MaxMp = mana.Max
        };
    }
}