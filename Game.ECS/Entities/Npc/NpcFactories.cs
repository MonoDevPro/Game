using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Npc;
using Game.ECS.Services;

namespace Game.ECS.Entities.Player;

public static class NpcFactories
{
    public static void DestroyNpc(this World world, Entity entity, ResourceStack<string> nameRegistry)
    {
        if (world.Has<NameHandle>(entity))
        {
            var nameRef = world.Get<NameHandle>(entity);
            nameRegistry.Unregister(nameRef.Value);
        }
        world.Destroy(entity);
    }
    
    public static Entity CreateNpc(this World world, NpcTemplate template, ResourceStack<string> nameRegistry, 
        int networkId, int mapId, int floor, Position position)
    {
        // Create the player entity with all necessary components
        var entity = world.Create(NpcArchetypes.NPC);
        
        // Set up components
        var components = new object[]
        {
            new NetworkId { Value = networkId },
            new NpcId { Value = template.NpcId },
            new GenderId { Value = template.Gender },
            new VocationId { Value = template.Vocation },
            
            new MapId { Value = mapId },
            new Floor { Level = (sbyte)floor },
            new NameHandle { Value = nameRegistry.Register(template.Name) },
            
            new Position { X = position.X, Y = position.Y },
            new Direction { X = 0, Y = 1 },
            new Speed { Value = 0f },
            new Movement { Timer = 0f },
            new Health { Current = template.Vitals.CurrentHp, Max = template.Vitals.MaxHp, RegenerationRate = template.Vitals.HpRegen },
            new Mana { Current = template.Vitals.CurrentMp, Max = template.Vitals.MaxMp, RegenerationRate = template.Vitals.MpRegen },
            new Walkable { BaseSpeed = 3f, CurrentModifier = template.Stats.MovementSpeed },
            new CombatStats 
            { 
                AttackPower = template.Stats.PhysicalAttack,
                MagicPower = template.Stats.MagicAttack,
                Defense = template.Stats.PhysicalDefense,
                MagicDefense = template.Stats.MagicDefense,
                AttackRange = 1.5f,
                AttackSpeed = template.Stats.AttackSpeed > 0 ? template.Stats.AttackSpeed : 1f
            },
            new CombatState 
            { 
                AttackCooldownTimer = 0f,
                CastTimer = 0f
            },
            new Input { },
            new DirtyFlags { }
        };
        world.SetRange(entity, components);
        return entity;
    }
}