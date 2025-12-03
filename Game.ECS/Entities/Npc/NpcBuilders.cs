using Arch.Core;
using Game.Domain.Enums;
using Game.ECS.Schema.Archetypes;
using Game.ECS.Schema.Components;
using Game.ECS.Schema.Templates;
using Game.ECS.Services.Index;

namespace Game.ECS.Entities.Npc;

public static class NpcBuilder
{
    /// <summary>
    /// Builds a snapshot for NPC spawn packet (full data).
    /// </summary>
    public static NpcSnapshot BuildNpcData(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var direction = ref world.Get<Direction>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);

        return new NpcSnapshot(
            NetworkId: networkId.Value,
            MapId: mapId.Value,
            Name: string.Empty, // Name will be set by service
            X: position.X,
            Y: position.Y,
            Floor: floor.Value,
            DirX: direction.X,
            DirY: direction.Y,
            Hp: health.Current,
            MaxHp: health.Max,
            Mp: mana.Current,
            MaxMp: mana.Max
        );
    }

    /// <summary>
    /// Builds a state snapshot for NPC state updates (movement).
    /// </summary>
    public static NpcStateSnapshot BuildNpcStateSnapshot(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var speed = ref world.Get<Speed>(entity);
        ref var direction = ref world.Get<Direction>(entity);

        return new NpcStateSnapshot(
            NetworkId: networkId.Value,
            X: position.X,
            Y: position.Y,
            Floor: floor.Value,
            Speed: speed.Value,
            DirectionX: direction.X,
            DirectionY: direction.Y
        );
    }

    /// <summary>
    /// Builds a vitals snapshot for NPC health/mana updates.
    /// </summary>
    public static NpcVitalsSnapshot BuildNpcVitalsSnapshot(this World world, Entity entity)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);

        return new NpcVitalsSnapshot(
            NetworkId: networkId.Value,
            CurrentHp: health.Current,
            MaxHp: health.Max,
            CurrentMp: mana.Current,
            MaxMp: mana.Max
        );
    }

    /// <summary>
    /// Creates an NPC entity from a network snapshot (for clients).
    /// </summary>
    public static Entity CreateNPC(this World world, in NpcSnapshot snapshot)
    {
        var entity = world.Create(NpcArchetypes.NPC);
        
        world.SetRange(entity, new object[]
        {
            new NetworkId { Value = snapshot.NetworkId },
            new MapId { Value = snapshot.MapId },
            new Floor { Value = snapshot.Floor },
            new Position { X = snapshot.X, Y = snapshot.Y },
            new AIControlled(),
            new UniqueID { Value = snapshot.NetworkId }, // Using NetworkId as UniqueId for client-side
            new GenderId { Value = 0 },
            new VocationId { Value = 0 },
            new NameHandle { Value = default }, // Client doesn't manage name registry
            new Brain { CurrentState = AIState.Idle, StateTimer = 0f, CurrentTarget = Entity.Null },
            new AIBehaviour
            {
                Type = BehaviorType.Passive,
                VisionRange = 5f,
                AttackRange = 1f,
                LeashRange = 10f,
                PatrolRadius = 0f,
                IdleDurationMin = 2f,
                IdleDurationMax = 5f
            },
            new NavigationAgent { Destination = null, StoppingDistance = 0f, IsPathPending = false },
            new Direction { X = snapshot.DirX, Y = snapshot.DirY },
            new Speed { Value = 0f },
            new Walkable { BaseSpeed = 3f, CurrentModifier = 1f },
            new CombatStats
            {
                AttackPower = 10,
                MagicPower = 5,
                Defense = 5,
                MagicDefense = 3,
                AttackRange = 1f,
                AttackSpeed = 1f
            },
            new CombatState { AttackCooldownTimer = 0f, CastTimer = 0f },
            new Health { Current = snapshot.Hp, Max = snapshot.MaxHp, RegenerationRate = 0.1f },
            new Mana { Current = snapshot.Mp, Max = snapshot.MaxMp, RegenerationRate = 0.1f },
            new SpawnPoint(snapshot.MapId, snapshot.X, snapshot.Y, snapshot.Floor)
        });
        
        return entity;
    }

    public static NpcTemplate BuildNpcTemplate(this World world, Entity entity, ResourceIndex<string> resources, NpcTemplate? existingTemplate = null)
    {
        ref var uniqueId = ref world.Get<UniqueID>(entity);
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
        ref var name = ref world.Get<NameHandle>(entity);
        ref var genderId = ref world.Get<GenderId>(entity);
        ref var vocationId = ref world.Get<VocationId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var direction = ref world.Get<Direction>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        
        existingTemplate ??= new NpcTemplate();
        existingTemplate.Id = uniqueId.Value;
        existingTemplate.IdentityTemplate = new IdentityTemplate(
            NetworkId: networkId.Value,
            Name: resources.Get(name.Value),
            Gender: (Gender)genderId.Value,
            Vocation: (VocationType)vocationId.Value);
        existingTemplate.LocationTemplate = new LocationTemplate(
            MapId: mapId.Value,
            Floor: floor.Value,
            X: position.X,
            Y: position.Y);
        existingTemplate.StatsTemplate = new StatsTemplate(
            MovementSpeed: 1f,
            AttackSpeed: 1f,
            PhysicalAttack: 10,
            MagicAttack: 5,
            PhysicalDefense: 5,
            MagicDefense: 3);
        existingTemplate.VitalsTemplate = new VitalsTemplate(
            CurrentHp: health.Current,
            MaxHp: health.Max,
            CurrentMp: mana.Current,
            MaxMp: mana.Max,
            HpRegen: 0.1f,
            MpRegen: 0.1f);
        existingTemplate.BehaviorTemplate = new BehaviorTemplate(
            BehaviorType: BehaviorType.Passive,
            VisionRange: 5f,
            AttackRange: 1f,
            LeashRange: 10f,
            PatrolRadius: 3f,
            IdleDurationMin: 2f,
            IdleDurationMax: 5f);
        existingTemplate.DirectionTemplate = new DirectionTemplate(
            DirX: direction.X,
            DirY: direction.Y);
        return existingTemplate;
    }
}