using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Archetypes;
using Game.ECS.Entities.Repositories;

namespace Game.ECS.Entities.Factories;

/// <summary>
/// Dados completos de um jogador (usado no spawn).
/// </summary>
public readonly record struct PlayerData(
    int PlayerId, int NetworkId,
    string Name, byte Gender, byte Vocation,
    int PosX, int PosY, int PosZ,
    int FacingX, int FacingY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense,
    int MapId = 0);

public readonly record struct PlayerStateData(
    int NetworkId,
    int PositionX,
    int PositionY,
    int PositionZ,
    int VelocityX,
    int VelocityY,
    float Speed,
    int FacingX,
    int FacingY);

public readonly record struct PlayerVitalsData(
    int NetworkId, 
    int CurrentHp, 
    int MaxHp, 
    int CurrentMp, 
    int MaxMp);

public static partial class EntityFactory
{
    public static Entity CreatePlayer(this World world, PlayerIndex? index, in PlayerData data)
    {
        var entity = world.Create(GameArchetypes.PlayerCharacter);
        var components = new object[]
        {
            new NetworkId { Value = data.NetworkId },
            new PlayerId { Value = data.PlayerId },
            new MapId { Value = data.MapId },
            new PlayerInfo { GenderId = data.Gender, VocationId = data.Vocation},
            new Position { X = data.PosX, Y = data.PosY, Z = data.PosZ },
            new Facing { DirectionX = data.FacingX, DirectionY = data.FacingY },
            new Velocity { DirectionX = 0, DirectionY = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = data.Hp, Max = data.MaxHp, RegenerationRate = data.HpRegen },
            new Mana { Current = data.Mp, Max = data.MaxMp, RegenerationRate = data.MpRegen },
            new Walkable { BaseSpeed = 5f, CurrentModifier = data.MovementSpeed },
            new Attackable { BaseSpeed = 1f, CurrentModifier = data.AttackSpeed },
            new AttackPower { Physical = data.PhysicalAttack, Magical = data.MagicAttack },
            new Defense { Physical = data.PhysicalDefense, Magical = data.MagicDefense },
            new CombatState { InCombat = false, TimeSinceLastHit = SimulationConfig.HealthRegenDelayAfterCombat },
            new PlayerInput { },
            new PlayerControlled(),
            new DirtyFlags()
        };
        world.SetRange(entity, components);
        index?.AddMapping(data.NetworkId, entity);
        return entity;
    }
    
    public static bool TryDestroyPlayer(this World world, PlayerIndex index, int networkId)
    {
        if (!index.TryGetEntity(networkId, out var entity))
            return false;
        index.RemoveByEntity(entity);
        world.Destroy(entity);
        return true;
    }
    
    public static bool TryBuildPlayerSnapshot(this World world, Entity entity, out PlayerData data)
    {
        if (!world.IsAlive(entity))
        {
            data = default;
            return false;
        }
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var playerId = ref world.Get<PlayerId>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
        ref var info = ref world.Get<PlayerInfo>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var facing = ref world.Get<Facing>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        ref var walkable = ref world.Get<Walkable>(entity);
        ref var attackable = ref world.Get<Attackable>(entity);
        ref var attackPower = ref world.Get<AttackPower>(entity);
        ref var defense = ref world.Get<Defense>(entity);

        data = new PlayerData
        {
            Vocation = info.VocationId, Gender = info.GenderId,
            NetworkId = networkId.Value, PlayerId = playerId.Value, MapId = mapId.Value,
            PosX = position.X, PosY = position.Y, PosZ = position.Z,
            FacingX = facing.DirectionX, FacingY = facing.DirectionY,
            Hp = health.Current, MaxHp = health.Max, HpRegen = health.RegenerationRate,
            Mp = mana.Current, MaxMp = mana.Max, MpRegen = mana.RegenerationRate,
            MovementSpeed = walkable.CurrentModifier, AttackSpeed = attackable.CurrentModifier,
            PhysicalAttack = attackPower.Physical, MagicAttack = attackPower.Magical,
            PhysicalDefense = defense.Physical, MagicDefense = defense.Magical
        };
        return true;
    }
    
    public static bool TryBuildPlayerStateSnapshot(this World world, Entity entity, out PlayerStateData data)
    {
        if (!world.IsAlive(entity))
        {
            data = default;
            return false;
        }
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var facing = ref world.Get<Facing>(entity);
        ref var velocity = ref world.Get<Velocity>(entity);
        
        data = new PlayerStateData
        {
            NetworkId = networkId.Value,
            PositionX = position.X,
            PositionY = position.Y,
            PositionZ = position.Z,
            VelocityX = velocity.DirectionX,
            VelocityY = velocity.DirectionY,
            Speed = velocity.Speed,
            FacingX = facing.DirectionX,
            FacingY = facing.DirectionY,
        };
        return true;
    }
    
    public static bool TryBuildPlayerVitalsSnapshot(this World world, Entity entity, out PlayerVitalsData data)
    {
        if (!world.IsAlive(entity))
        {
            data = default;
            return false;
        }
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);

        data = new PlayerVitalsData
        {
            NetworkId = networkId.Value,
            CurrentHp = health.Current,
            MaxHp = health.Max,
            CurrentMp = mana.Current,
            MaxMp = mana.Max
        };
        return true;
    }
}