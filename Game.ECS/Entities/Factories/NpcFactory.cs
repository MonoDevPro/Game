using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Archetypes;
using MemoryPack;

namespace Game.ECS.Entities.Factories;

/// <summary>
/// Dados de um NPC (controlado por IA).
/// </summary>
[MemoryPackable]
public readonly partial record struct NPCSnapshot(
    int NetworkId,
    int PositionX, int PositionY, int PositionZ,
    int Hp, int MaxHp, float HpRegen,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense,
    int MapId = 0);

// ============================================
// NPC Snapshots
// ============================================

[MemoryPackable]
public readonly partial record struct NpcStateSnapshot(
    int NetworkId,
    int PositionX,
    int PositionY,
    int PositionZ,
    int FacingX,
    int FacingY,
    float Speed,
    int CurrentHp,
    int MaxHp);

public static class NpcFactory
{
    /// <summary>
    /// Cria um NPC com IA controlada.
    /// </summary>
    public static Entity CreateNPC(this World world, in NPCSnapshot data)
    {
        var entity = world.Create(GameArchetypes.NPCCharacter);
        var components = new object[]
        {
            new NetworkId { Value = data.NetworkId },
            new MapId { Value = data.MapId },
            new Position { X = data.PositionX, Y = data.PositionY, Z = data.PositionZ },
            new Facing { DirectionX = 0, DirectionY = 0 },
            new Velocity { DirectionX = 0, DirectionY = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = data.Hp, Max = data.MaxHp, RegenerationRate = data.HpRegen },
            new Walkable { BaseSpeed = 3f, CurrentModifier = 1f },
            new AttackPower { Physical = data.PhysicalAttack, Magical = data.MagicAttack },
            new Defense { Physical = data.PhysicalDefense, Magical = data.MagicDefense },
            new CombatState { },
            new AIControlled(),
            new AIState
            {
                DecisionCooldown = 0f,
                CurrentBehavior = AIBehavior.Wander,
                TargetNetworkId = 0
            },
            new DirtyFlags()
        };
        world.SetRange(entity, components);
        return entity;
    }
    
    public static NPCSnapshot BuildNPCSnapshot(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var attackPower = ref world.Get<AttackPower>(entity);
        ref var defense = ref world.Get<Defense>(entity);

        return new NPCSnapshot
        {
            NetworkId = networkId.Value, MapId = mapId.Value,
            PositionX = position.X, PositionY = position.Y, PositionZ = position.Z,
            Hp = health.Current, MaxHp = health.Max, HpRegen = health.RegenerationRate,
            PhysicalAttack = attackPower.Physical, MagicAttack = attackPower.Magical,
            PhysicalDefense = defense.Physical, MagicDefense = defense.Magical
        };
    }
    
    public static NpcStateSnapshot BuildNpcStateSnapshot(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var facing = ref world.Get<Facing>(entity);
        ref var walkable = ref world.Get<Walkable>(entity);
        ref var health = ref world.Get<Health>(entity);

        return new NpcStateSnapshot
        {
            NetworkId = networkId.Value,
            PositionX = position.X,
            PositionY = position.Y,
            PositionZ = position.Z,
            FacingX = facing.DirectionX,
            FacingY = facing.DirectionY,
            Speed = walkable.BaseSpeed * walkable.CurrentModifier,
            CurrentHp = health.Current,
            MaxHp = health.Max
        };
    }
}