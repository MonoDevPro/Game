using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Archetypes;
using Game.ECS.Services;
using MemoryPack;

namespace Game.ECS.Entities.Factories;

/// <summary>
/// Dados completos de um jogador (usado no spawn).
/// </summary>
[MemoryPackable]
public readonly partial record struct PlayerSnapshot(
    int PlayerId, int NetworkId,
    string Name, byte Gender, byte Vocation,
    int SpawnX, int SpawnY, int SpawnZ,
    int FacingX, int FacingY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense,
    int MapId = 0);

[MemoryPackable]
public readonly partial record struct PlayerStateSnapshot(
    int NetworkId,
    int PositionX, 
    int PositionY, 
    int PositionZ, 
    int FacingX, 
    int FacingY, 
    float Speed);

[MemoryPackable]
public readonly partial record struct PlayerVitalsSnapshot(
    int NetworkId, 
    int CurrentHp, 
    int MaxHp, 
    int CurrentMp, 
    int MaxMp);

[MemoryPackable]
public readonly partial record struct PlayerDespawnSnapshot(int Id);

[MemoryPackable]
public readonly partial record struct PlayerPositionSnapshot(
    int NetworkId, 
    int PositionX, 
    int PositionY, 
    int PositionZ);

[MemoryPackable]
public readonly partial record struct PlayerMapChangeSnapshot(
    int NetworkId, 
    int NewMapId, 
    int PositionX, 
    int PositionY, 
    int PositionZ);

public static partial class EntityFactory
{
    public static Entity CreatePlayer(this World world, EntityIndex<int> index, in PlayerSnapshot data)
    {
        var entity = world.Create(GameArchetypes.PlayerCharacter);
        var components = new object[]
        {
            new NetworkId { Value = data.NetworkId },
            new PlayerId { Value = data.PlayerId },
            new MapId { Value = data.MapId },
            new Position { X = data.SpawnX, Y = data.SpawnY, Z = data.SpawnZ },
            new Facing { DirectionX = data.FacingX, DirectionY = data.FacingY },
            new Velocity { DirectionX = 0, DirectionY = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = data.Hp, Max = data.MaxHp, RegenerationRate = data.HpRegen },
            new Mana { Current = data.Mp, Max = data.MaxMp, RegenerationRate = data.MpRegen },
            new Walkable { BaseSpeed = 5f, CurrentModifier = data.MovementSpeed },
            new Attackable { BaseSpeed = 1f, CurrentModifier = data.AttackSpeed },
            new AttackPower { Physical = data.PhysicalAttack, Magical = data.MagicAttack },
            new Defense { Physical = data.PhysicalDefense, Magical = data.MagicDefense },
            new CombatState { },
            new PlayerInput { },
            new PlayerControlled(),
            new DirtyFlags()
        };
        world.SetRange(entity, components);
        index.AddMapping(data.PlayerId, entity);
        return entity;
    }
    
    public static Entity CreateLocalPlayer(this World world, EntityIndex<int> index, in PlayerSnapshot data)
    {
        var entity = world.CreatePlayer(index, data);
        world.Add<LocalPlayerTag>(entity);
        return entity;
    }
    
    public static Entity CreateRemotePlayer(this World world, EntityIndex<int> index, in PlayerSnapshot data)
    {
        var entity = world.CreatePlayer(index, data);
        world.Add<RemotePlayerTag>(entity);
        return entity;
    }
    
    public static bool TryDestroyPlayer(this World world, EntityIndex<int> index, Entity entity)
    {
        if (!world.IsAlive(entity) || !world.Has<PlayerId>(entity))
            return false;
        
        ref var playerId = ref world.Get<PlayerId>(entity);
        index.RemoveByEntity(entity);
        world.Destroy(entity);
        return true;
    }
    
    public static bool TryBuildPlayerSnapshot(this World world, Entity entity, out PlayerSnapshot snapshot)
    {
        if (!world.IsAlive(entity))
        {
            snapshot = default;
            return false;
        }
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var playerId = ref world.Get<PlayerId>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var facing = ref world.Get<Facing>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        ref var walkable = ref world.Get<Walkable>(entity);
        ref var attackable = ref world.Get<Attackable>(entity);
        ref var attackPower = ref world.Get<AttackPower>(entity);
        ref var defense = ref world.Get<Defense>(entity);

        snapshot = new PlayerSnapshot
        {
            NetworkId = networkId.Value, PlayerId = playerId.Value, MapId = mapId.Value,
            SpawnX = position.X, SpawnY = position.Y, SpawnZ = position.Z,
            FacingX = facing.DirectionX, FacingY = facing.DirectionY,
            Hp = health.Current, MaxHp = health.Max, HpRegen = health.RegenerationRate,
            Mp = mana.Current, MaxMp = mana.Max, MpRegen = mana.RegenerationRate,
            MovementSpeed = walkable.CurrentModifier, AttackSpeed = attackable.CurrentModifier,
            PhysicalAttack = attackPower.Physical, MagicAttack = attackPower.Magical,
            PhysicalDefense = defense.Physical, MagicDefense = defense.Magical
        };
        return true;
    }
    
    public static bool TryBuildPlayerStateSnapshot(this World world, Entity entity, out PlayerStateSnapshot snapshot)
    {
        if (!world.IsAlive(entity))
        {
            snapshot = default;
            return false;
        }
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var facing = ref world.Get<Facing>(entity);
        ref var walkable = ref world.Get<Walkable>(entity);
        
        snapshot = new PlayerStateSnapshot
        {
            NetworkId = networkId.Value,
            PositionX = position.X,
            PositionY = position.Y,
            PositionZ = position.Z,
            FacingX = facing.DirectionX,
            FacingY = facing.DirectionY,
            Speed = walkable.BaseSpeed * walkable.CurrentModifier
        };
        return true;
    }
    
    public static bool TryBuildPlayerVitalsSnapshot(this World world, Entity entity, out PlayerVitalsSnapshot snapshot)
    {
        if (!world.IsAlive(entity))
        {
            snapshot = default;
            return false;
        }
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);

        snapshot = new PlayerVitalsSnapshot
        {
            NetworkId = networkId.Value,
            CurrentHp = health.Current,
            MaxHp = health.Max,
            CurrentMp = mana.Current,
            MaxMp = mana.Max
        };
        return true;
    }
    
    public static PlayerDespawnSnapshot BuildPlayerDespawnSnapshot(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        return new PlayerDespawnSnapshot(networkId.Value);
    }
    
    public static bool TryBuildPlayerPositionSnapshot(this World world, Entity entity, out PlayerPositionSnapshot snapshot)
    {
        if (!world.IsAlive(entity))
        {
            snapshot = default;
            return false;
        }
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var position = ref world.Get<Position>(entity);

        snapshot = new PlayerPositionSnapshot
        {
            NetworkId = networkId.Value,
            PositionX = position.X,
            PositionY = position.Y,
            PositionZ = position.Z
        };
        return true;
    }
    
    public static bool TryBuildPlayerMapChangeSnapshot(this World world, Entity entity, out PlayerMapChangeSnapshot snapshot)
    {
        if (!world.IsAlive(entity))
        {
            snapshot = default;
            return false;
        }
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
        ref var position = ref world.Get<Position>(entity);

        snapshot = new PlayerMapChangeSnapshot
        {
            NetworkId = networkId.Value,
            NewMapId = mapId.Value,
            PositionX = position.X,
            PositionY = position.Y,
            PositionZ = position.Z
        };
        return true;
    }
}