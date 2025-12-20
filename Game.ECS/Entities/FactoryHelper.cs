using Arch.Core;
using Game.DTOs.Game.Npc;
using Game.DTOs.Game.Player;
using Game.ECS.Archetypes;
using Game.ECS.Components;

namespace Game.ECS.Entities;

public static class FactoryHelper
{
    public static Entity CreatePlayer(this World world, ref PlayerData template)
    {
        var entity = world.Create(PlayerArchetypes.PlayerArchetype);
        
        var components = new object[]
        {
            new NetworkId { Value = template.NetworkId },
            new MapId { Value = template.MapId },
            new Position { X = template.X, Y = template.Y, Z =  template.Z },
            
            new PlayerControlled { },
            new UniqueID { Value = template.PlayerId },
            new GenderId { Value = template.Gender },
            new VocationId { Value = template.Vocation },
            
            new Input { },
            
            new Direction { X = template.DirX, Y = template.DirY },
            new Speed { Value = 0f },
            
            new Walkable { BaseSpeed = 1f, CurrentModifier = template.MovementSpeed },
            new SpatialAnchor
            {
                MapId = template.MapId,
                Position = new Position { X = template.X, Y = template.Y, Z = template.Z },
                IsTracked = false
            },
            
            new CombatStats
            { 
                AttackPower = template.PhysicalAttack,
                MagicPower = template.MagicAttack,
                Defense = template.PhysicalDefense,
                MagicDefense = template.MagicDefense,
                AttackRange = 1.5f,
                AttackSpeed = template.AttackSpeed > 0 ? template.AttackSpeed : 1f
            },
            new CombatState { CooldownTimer = 0f },
            
            new Health { Current = template.Hp, Max = template.MaxHp, RegenerationRate = template.HpRegen },
            new Mana { Current = template.Mp, Max = template.MaxMp, RegenerationRate = template.MpRegen },
            
            new SpawnPoint(template.MapId, template.X, template.Y, template.Z),
        };
        world.SetRange(entity, components);
        return entity;
    }
    
    public static Entity CreateNpc(this World world, ref NpcData template, ref Behaviour behaviour)
    {
        // Create the entity with all necessary components
        var entity = world.Create(NpcArchetypes.NPC);

        // Set up components
        var components = new object[]
        {
            // Set from systems
            new NetworkId { Value = template.NetworkId },
            new MapId { Value = template.MapId },
            new Position { X = template.X, Y = template.Y, Z =  template.Z },

            // Identity
            new AIControlled { },
            new UniqueID() { Value = template.NpcId }, // Using NetworkId as UniqueId for server-side

            // AI
            new Brain { CurrentState = AIState.Idle, StateTimer = 0f, CurrentTarget = Entity.Null },
            new AIBehaviour
            {
                Type = behaviour.BehaviorType,
                VisionRange = behaviour.VisionRange,
                AttackRange = behaviour.AttackRange,
                LeashRange = behaviour.LeashRange,
                PatrolRadius = behaviour.PatrolRadius,
                IdleDurationMin = behaviour.IdleDurationMin,
                IdleDurationMax = behaviour.IdleDurationMax
            },
            new NavigationAgent { TargetPosition = default, StoppingDistance = 0f, IsPathPending = false },

            new Direction { X = template.DirX, Y = template.DirY },
            new Speed { Value = 0f },

            new Walkable { BaseSpeed = 1f, CurrentModifier = template.MovementSpeed },
            new SpatialAnchor
            {
                MapId = template.MapId,
                Position = new Position { X = template.X, Y = template.Y, Z = template.Z },
                IsTracked = false
            },

            new CombatStats
            {
                AttackPower = template.PhysicalAttack,
                MagicPower = template.MagicAttack,
                Defense = template.PhysicalDefense,
                MagicDefense = template.MagicDefense,
                AttackRange = behaviour.AttackRange,
                AttackSpeed = template.AttackSpeed > 0 ? template.AttackSpeed : 1f
            },
            new CombatState { CooldownTimer = 0f },

            new Health { Current = template.Hp, Max = template.MaxHp, RegenerationRate = template.HpRegen },
            new Mana { Current = template.Mp, Max = template.MaxMp, RegenerationRate = template.MpRegen },
            
            new SpawnPoint(template.MapId, template.X, template.Y, template.Z),
        };
        
        world.SetRange(entity, components);
        return entity;
    }
}