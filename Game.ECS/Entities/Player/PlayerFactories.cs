using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Npc;
using Game.ECS.Schema.Archetypes;
using Game.ECS.Schema.Components;
using Game.ECS.Schema.Templates;
using Game.ECS.Services;
using Game.ECS.Services.Index;

namespace Game.ECS.Entities.Player;

public static class PlayerFactories
{
    public static Entity CreatePlayer(this World world, ResourceIndex<string> nameRegistry, PlayerTemplate template)
    {
        // Create the player entity with all necessary components
        var entity = world.Create(PlayerArchetypes.PlayerArchetype);
        
        // Set up components
        var components = new object[]
        {
            // Set from systems
            new NetworkId { /* Value will be set by NetworkEntitySystem */ },
            new MapId { /* Value will be set by SpawnSystem */  },
            new Floor { /* Value will be set by SpawnSystem */ },
            new Position { /* Value will be set by SpawnSystem */ },
            
            // Identity
            new PlayerControlled { },
            new UniqueID { Value = template.Id },
            new GenderId { Value = (byte)template.IdentityTemplate.Gender },
            new VocationId { Value = (byte)template.IdentityTemplate.Vocation },
            new NameHandle { Value = nameRegistry.Register(template.IdentityTemplate.Name) },
            
            // Input
            new Input { },
            
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
            new CombatState { AttackCooldownTimer = 0f, CastTimer = 0f },
            
            // Vitals
            new Health { Current = template.VitalsTemplate.CurrentHp, Max = template.VitalsTemplate.MaxHp, RegenerationRate = template.VitalsTemplate.HpRegen },
            new Mana { Current = template.VitalsTemplate.CurrentMp, Max = template.VitalsTemplate.MaxMp, RegenerationRate = template.VitalsTemplate.MpRegen },
        };
        world.SetRange(entity, components);
        return entity;
    }
}