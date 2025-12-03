using Arch.Core;
using Game.ECS.Schema.Archetypes;
using Game.ECS.Schema.Components;
using Game.ECS.Schema.Templates;
using Game.ECS.Services.Index;

namespace Game.ECS.Entities.Npc;

public static class NpcFactories
{
    public static Entity CreateNpc(this World world, NpcTemplate template, ResourceIndex<string> nameRegistry)
    {
        // Create the entity with all necessary components
        var entity = world.Create(NpcArchetypes.NPC);

        // Set up components
        var components = new object[]
        {
            // Set from systems
            new NetworkId
            {
                /* Value will be set by NetworkEntitySystem */
            },
            new MapId
            {
                /* Value will be set by SpawnSystem */
            },
            new Floor
            {
                /* Value will be set by SpawnSystem */
            },
            new Position
            {
                /* Value will be set by SpawnSystem */
            },

            // Identity
            new AIControlled { },
            new UniqueID() { Value = template.Id },
            new GenderId { Value = (byte)template.IdentityTemplate.Gender },
            new VocationId { Value = (byte)template.IdentityTemplate.Vocation },
            new NameHandle { Value = nameRegistry.Register(template.IdentityTemplate.Name) },

            // AI
            new AIBehaviour
            {
                Type = template.BehaviorTemplate.BehaviorType,
                VisionRange = template.BehaviorTemplate.VisionRange,
                AttackRange = template.BehaviorTemplate.AttackRange,
                LeashRange = template.BehaviorTemplate.LeashRange,
                PatrolRadius = template.BehaviorTemplate.PatrolRadius,
                IdleDurationMin = template.BehaviorTemplate.IdleDurationMin,
                IdleDurationMax = template.BehaviorTemplate.IdleDurationMax
            },

            // Transform
            new Direction { X = template.DirectionTemplate.DirX, Y = template.DirectionTemplate.DirY },
            new Speed { Value = 0f },

            // Movement
            new Walkable { BaseSpeed = 3f, CurrentModifier = template.StatsTemplate.MovementSpeed },

            // Combat
            new CombatStats
            {
                AttackPower = template.StatsTemplate.PhysicalAttack,
                MagicPower = template.StatsTemplate.MagicAttack,
                Defense = template.StatsTemplate.PhysicalDefense,
                MagicDefense = template.StatsTemplate.MagicDefense,
                AttackRange = 1.5f,
                AttackSpeed = template.StatsTemplate.AttackSpeed > 0 ? template.StatsTemplate.AttackSpeed : 1f
            },
            new CombatState
            {
                AttackCooldownTimer = 0f,
                CastTimer = 0f
            },

            // Vitals
            new Health
            {
                Current = template.VitalsTemplate.CurrentHp, Max = template.VitalsTemplate.MaxHp,
                RegenerationRate = template.VitalsTemplate.HpRegen
            },
            new Mana
            {
                Current = template.VitalsTemplate.CurrentMp, Max = template.VitalsTemplate.MaxMp,
                RegenerationRate = template.VitalsTemplate.MpRegen
            }
        };
        
        world.SetRange(entity, components);
        return entity;
    }

    public static readonly ComponentType[] NPC =
    [
        // Set from systems
        Component<NetworkId>.ComponentType, // Assigned by the networking system
        Component<MapId>.ComponentType,     // Assigned by the map management system
        Component<Position>.ComponentType,  // Assigned by the spawn system
        Component<Floor>.ComponentType,     // Assigned by the spawn system
        // Identity
        Component<AIControlled>.ComponentType,
        Component<UniqueID>.ComponentType,
        Component<GenderId>.ComponentType,
        Component<VocationId>.ComponentType,
        Component<NameHandle>.ComponentType,
        // AI
        Component<AIBehaviour>.ComponentType,
        // Transform
        Component<Direction>.ComponentType,
        Component<Speed>.ComponentType,
        // Movement
        Component<Walkable>.ComponentType,
        // Combat
        Component<CombatStats>.ComponentType,
        Component<CombatState>.ComponentType,
        // Vitals
        Component<Health>.ComponentType,
        Component<Mana>.ComponentType,
    ];
}
        
