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
            new NetworkId { Value = template.IdentityTemplate.NetworkId },
            new MapId { Value = template.LocationTemplate.MapId },
            new Floor { Value = (sbyte)template.LocationTemplate.Floor },
            new Position { X = template.LocationTemplate.X, Y = template.LocationTemplate.Y },

            // Identity
            new AIControlled { },
            new UniqueID() { Value = template.Id },
            new GenderId { Value = (byte)template.IdentityTemplate.Gender },
            new VocationId { Value = (byte)template.IdentityTemplate.Vocation },
            new NameHandle { Value = nameRegistry.Register(template.IdentityTemplate.Name) },

            // AI
            new Brain { CurrentState = AIState.Idle, StateTimer = 0f, CurrentTarget = Entity.Null },
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
            new NavigationAgent { Destination = null, StoppingDistance = 0f, IsPathPending = false },

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
                AttackRange = template.BehaviorTemplate.AttackRange,
                AttackSpeed = template.StatsTemplate.AttackSpeed > 0 ? template.StatsTemplate.AttackSpeed : 1f
            },
            new CombatState { AttackCooldownTimer = 0f, CastTimer = 0f },

            // Vitals
            new Health
            {
                Current = template.VitalsTemplate.CurrentHp, 
                Max = template.VitalsTemplate.MaxHp,
                RegenerationRate = template.VitalsTemplate.HpRegen
            },
            new Mana
            {
                Current = template.VitalsTemplate.CurrentMp, 
                Max = template.VitalsTemplate.MaxMp,
                RegenerationRate = template.VitalsTemplate.MpRegen
            },
            
            // Lifecycle - SpawnPoint for respawning
            new SpawnPoint(template.LocationTemplate.MapId, template.LocationTemplate.X, template.LocationTemplate.Y, (sbyte)template.LocationTemplate.Floor),
        };
        
        world.SetRange(entity, components);
        return entity;
    }
}