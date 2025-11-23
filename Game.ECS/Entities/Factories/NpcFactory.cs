using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Archetypes;
using Game.ECS.Entities.Data;

namespace Game.ECS.Entities.Factories;

public static partial class EntityFactory
{
    /// <summary>
    /// Cria um NPC com IA controlada.
    /// </summary>
    public static Entity CreateNPC(this World world, in NPCData data)
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
                Type = NpcBehaviorType.Aggressive,
                VisionRange = 6f,
                AttackRange = 1.25f,
                LeashRange = 12f,
                PatrolRadius = 5f,
                IdleDurationMin = 1.5f,
                IdleDurationMax = 3.5f
            },
            new NpcPatrol
            {
                HomePosition = new Position { X = data.PositionX, Y = data.PositionY, },
                Destination = new Position { X = data.PositionX, Y = data.PositionY, }, 
                Radius = 5f, 
                HasDestination = false
            },
        };
        world.SetRange(entity, components);
        return entity;
    }
}