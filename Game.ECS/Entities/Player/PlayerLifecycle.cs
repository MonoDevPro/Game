using System.Reflection.Metadata;
using Arch.Core;
using Arch.LowLevel;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.ECS.Entities.Player;

/// <summary>
/// Template for creating a player entity with all required data.
/// </summary>
public record PlayerTemplate(
    int PlayerId,
    int NetworkId,
    int MapId,
    string Name, 
    byte GenderId, 
    byte VocationId,
    int PosX,
    int PosY,
    sbyte Floor,
    sbyte DirX, 
    sbyte DirY,
    int Hp, 
    int MaxHp, 
    float HpRegen,
    int Mp, 
    int MaxMp, 
    float MpRegen,
    float MovementSpeed, 
    float AttackSpeed,
    int PhysicalAttack, 
    int MagicAttack,
    int PhysicalDefense, 
    int MagicDefense);

public static class PlayerLifecycle
{
    /// <summary>
    /// Arquétipo de jogador completo com todos os componentes necessários.
    /// Inclui componentes para movimento, combate, vitals, input e sincronização.
    /// </summary>
    public static readonly ComponentType[] PlayerArchetype =
    [
        Component<PlayerId>.ComponentType,
        Component<NetworkId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<GenderId>.ComponentType,
        Component<VocationId>.ComponentType,
        Component<PlayerInfo>.ComponentType,
        Component<Position>.ComponentType,
        Component<Floor>.ComponentType,
        Component<Direction>.ComponentType,
        Component<Velocity>.ComponentType,
        Component<Movement>.ComponentType,
        Component<Health>.ComponentType,
        Component<Mana>.ComponentType,
        Component<Walkable>.ComponentType,
        Component<CombatStats>.ComponentType,
        Component<CombatState>.ComponentType,
        Component<Input>.ComponentType,
        Component<DirtyFlags>.ComponentType,
        Component<PlayerControlled>.ComponentType,
    ];
    
    /// <summary>
    /// Creates a player entity from a template. Uses template values for position, floor, map, and network ID.
    /// </summary>
    public static Entity CreatePlayer(World world, Func<string, Handle<string>> resources, PlayerTemplate template)
    {
        var entity = world.Create(PlayerArchetype);
        
        var components = new object[]
        {
            new NetworkId { Value = template.NetworkId },
            new PlayerId { Value = template.PlayerId },
            new MapId { Value = template.MapId },
            new NameHandle { Value = resources(template.Name) },
            new GenderId { Value = template.GenderId },
            new VocationId { Value = template.VocationId },
            new PlayerInfo { GenderId = template.GenderId, VocationId = template.VocationId },
            new Position { X = template.PosX, Y = template.PosY },
            new Floor { Level = template.Floor },
            new Direction { DirectionX = template.DirX, DirectionY = template.DirY },
            new Velocity { X = 0, Y = 0, Speed = 0f },
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
            new DirtyFlags { },
            new PlayerControlled { },
        };
        
        world.SetRange(entity, components);
        return entity;
    }
    
    public static void DestroyPlayer(World world, Entity entity, Action<Handle<string>> resources)
    {
        if (world.Has<NameHandle>(entity))
        {
            var nameRef = world.Get<NameHandle>(entity);
            resources(nameRef.Value);
        }
        world.Destroy(entity);
    }
}