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
            new Position { X = data.PositionX, Y = data.PositionY, Z = data.PositionZ },
            new Facing { DirectionX = 0, DirectionY = 0 },
            new Velocity { DirectionX = 0, DirectionY = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = data.Hp, Max = data.MaxHp, RegenerationRate = data.HpRegen },
            new Walkable { BaseSpeed = 3f, CurrentModifier = 1f },
            new Attackable { BaseSpeed = 1f, CurrentModifier = 1f },
            new AttackPower { Physical = data.PhysicalAttack, Magical = data.MagicAttack },
            new Defense { Physical = data.PhysicalDefense, Magical = data.MagicDefense },
            new CombatState { InCombat = false, TimeSinceLastHit = SimulationConfig.HealthRegenDelayAfterCombat },
            new Input { },
            new AIControlled(),
            new NpcAIState { Current = NpcAIStateId.Idle, StateTime = 0f },
            new NpcTarget
            {
                Target = Entity.Null,
                TargetNetworkId = 0,
                LastKnownPosition = new Position
                {
                    X = data.PositionX,
                    Y = data.PositionY,
                    Z = data.PositionZ
                },
                DistanceSquared = 0f
            },
            new NpcBehavior
            {
                Type = NpcBehaviorType.Aggressive,
                VisionRange = 6f,
                AttackRange = 1.25f,
                LeashRange = 12f,
                PatrolRadius = 0f,
                IdleDurationMin = 1.5f,
                IdleDurationMax = 3.5f
            },
            new NpcPatrol
            {
                HomePosition = new Position
                {
                    X = data.PositionX,
                    Y = data.PositionY,
                    Z = data.PositionZ
                },
                Destination = new Position
                {
                    X = data.PositionX,
                    Y = data.PositionY,
                    Z = data.PositionZ
                },
                Radius = 0f,
                HasDestination = false
            },
            new DirtyFlags()
        };
        world.SetRange(entity, components);
        return entity;
    }
}