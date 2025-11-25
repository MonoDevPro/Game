using Arch.Core;
using Game.Domain.Templates;
using Game.ECS.Components;
using Game.ECS.Entities.Archetypes;
using Game.ECS.Entities.Data;

namespace Game.ECS.Entities.Factories;

public static partial class EntityFactory
{
    /// <summary>
    /// Cria um NPC com IA controlada e suporte a pathfinding A*.
    /// </summary>
    public static Entity CreateNPC(this World world, NpcTemplate template, Position pos, int floor, int mapId, int networkId)
    {
        var entity = world.Create(GameArchetypes.NPCCharacter);
        var components = new object[]
        {
            new NetworkId { Value = networkId },
            new MapId { Value = mapId },
            new Position { X = pos.X, Y = pos.Y },
            new Floor { Level = (sbyte)floor },
            new Facing { DirectionX = 0, DirectionY = 1 },
            new Velocity { X = 0, Y = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = template.BaseHp, Max = template.BaseHp, RegenerationRate = template.Stats.HpRegen },
            new Mana { Current = template.BaseMp, Max = template.BaseMp, RegenerationRate = template.Stats.MpRegen },
            new Walkable { BaseSpeed = 2f, CurrentModifier = template.Stats.MovementSpeed },
            new Attackable { BaseSpeed = 1f, CurrentModifier = template.Stats.AttackSpeed },
            new AttackPower { Physical = template.Stats.PhysicalAttack, Magical = template.Stats.MagicAttack },
            new Defense { Physical = template.Stats.PhysicalDefense, Magical = template.Stats.MagicDefense },
            new CombatState { InCombat = false, TimeSinceLastHit = SimulationConfig.HealthRegenDelayAfterCombat },
            new Input { },
            new DirtyFlags { },
            new AIControlled { },
            new NpcInfo { GenderId = (byte)template.Gender, VocationId = (byte)template.Vocation },
            new NpcType { TemplateId = template.Id },
            new NpcBrain { CurrentState = NpcState.Idle, StateTimer = 0f, CurrentTarget = Entity.Null },
            new NavigationAgent { Destination = null, StoppingDistance = 0f, IsPathPending = false },
            new NpcAIState { Current = NpcAIStateId.Idle, StateTime = 0f },
            new NpcTarget { Target = Entity.Null, TargetNetworkId = -1, LastKnownPosition = default, DistanceSquared = 0f },
            new NpcBehavior
            {
                Type = (NpcBehaviorType)template.Behavior.Type,
                VisionRange = template.Behavior.VisionRange,
                AttackRange = template.Behavior.AttackRange,
                LeashRange = template.Behavior.LeashRange,
                PatrolRadius = template.Behavior.PatrolRadius,
                IdleDurationMin = template.Behavior.IdleDurationMin,
                IdleDurationMax = template.Behavior.IdleDurationMax
            },
            new NpcPatrol
            {
                HomePosition = new Position { X = pos.X, Y = pos.Y, },
                Destination = new Position { X = pos.X, Y = pos.Y, }, 
                Radius = template.Behavior.PatrolRadius,
                HasDestination = false
            },
            NpcPath.CreateDefault(),
        };
        world.SetRange(entity, components);
        return entity;
    }

    /// <summary>
    /// Cria um NPC com IA controlada e suporte a pathfinding A* (Legacy/Client).
    /// </summary>
    public static Entity CreateNPC(this World world, in NPCData data, in NpcBehaviorData behaviorData)
    {
        var entity = world.Create(GameArchetypes.NPCCharacter);
        var components = new object[]
        {
            new NetworkId { Value = data.NetworkId },
            new MapId { Value = data.MapId },
            new Position { X = data.PositionX, Y = data.PositionY },
            new Floor { Level = data.Floor },
            new Facing { DirectionX = data.FacingX, DirectionY = data.FacingY },
            new Velocity { X = 0, Y = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = data.Hp, Max = data.MaxHp, RegenerationRate = data.HpRegen },
            new Mana { Current = data.Mp, Max = data.MaxMp, RegenerationRate = data.MpRegen },
            new Walkable { BaseSpeed = 2f, CurrentModifier = data.MovementSpeed },
            new Attackable { BaseSpeed = 1f, CurrentModifier = data.AttackSpeed },
            new AttackPower { Physical = data.PhysicalAttack, Magical = data.MagicAttack },
            new Defense { Physical = data.PhysicalDefense, Magical = data.MagicDefense },
            new CombatState { InCombat = false, TimeSinceLastHit = SimulationConfig.HealthRegenDelayAfterCombat },
            new Input { },
            new DirtyFlags { },
            new AIControlled { },
            new NpcInfo { GenderId = data.Gender, VocationId = data.Vocation },
            new NpcAIState { Current = NpcAIStateId.Idle, StateTime = 0f },
            new NpcTarget { Target = Entity.Null, TargetNetworkId = -1, LastKnownPosition = default, DistanceSquared = 0f },
            new NpcBehavior
            {
                Type = (NpcBehaviorType)behaviorData.BehaviorType,
                VisionRange = behaviorData.VisionRange,
                AttackRange = behaviorData.AttackRange,
                LeashRange = behaviorData.LeashRange,
                PatrolRadius = behaviorData.PatrolRadius,
                IdleDurationMin = behaviorData.IdleDurationMin,
                IdleDurationMax = behaviorData.IdleDurationMax
            },
            new NpcPatrol
            {
                HomePosition = new Position { X = data.PositionX, Y = data.PositionY, },
                Destination = new Position { X = data.PositionX, Y = data.PositionY, }, 
                Radius = behaviorData.PatrolRadius,
                HasDestination = false
            },
            NpcPath.CreateDefault(),
        };
        world.SetRange(entity, components);
        return entity;
    }
}