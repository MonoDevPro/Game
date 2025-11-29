using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.ECS.Entities.Player;

public static class PlayerFactories
{
    public static void DestroyPlayer(this World world, Entity entity, ResourceStack<string> nameRegistry)
    {
        if (world.Has<NameHandle>(entity))
        {
            var nameRef = world.Get<NameHandle>(entity);
            nameRegistry.Unregister(nameRef.Value);
        }
        world.Destroy(entity);
    }
    
    public static Entity CreatePlayer(this World world, ResourceStack<string> nameRegistry, PlayerTemplate template, 
        int networkId, int mapId, int floor, Position position)
    {
        // Create the player entity with all necessary components
        var entity = world.Create(PlayerArchetypes.PlayerArchetype);
        
        // Set up components
        var components = new object[]
        {
            new NetworkId { Value = networkId },
            new PlayerId { Value = template.PlayerId },
            new GenderId { Value = template.GenderId },
            new VocationId { Value = template.VocationId },
            
            new MapId { Value = mapId },
            new Floor { Level = (sbyte)floor },
            new NameHandle { Value = nameRegistry.Register(template.Name) },
            
            new Position { X = position.X, Y = position.Y },
            new Direction { X = template.DirX, Y = template.DirY },
            new Speed { Value = 0f },
            new Movement { Timer = 0f },
            new Health { Current = template.Hp, Max = template.MaxHp, RegenerationRate = template.HpRegen },
            new Mana { Current = template.Mp, Max = template.MaxMp, RegenerationRate = template.MpRegen },
            new Walkable { BaseSpeed = 3f, CurrentModifier = template.MovementSpeed },
            new CombatStats 
            { 
                AttackPower = template.PhysicalAttack,
                MagicPower = template.MagicAttack,
                Defense = template.PhysicalDefense,
                MagicDefense = template.MagicDefense,
                AttackRange = 1.5f,
                AttackSpeed = template.AttackSpeed > 0 ? template.AttackSpeed : 1f
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