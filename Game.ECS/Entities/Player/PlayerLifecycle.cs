using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.ECS.Entities.Player;

public record PlayerTemplate(
    int PlayerId, string Name, byte GenderId, byte VocationId,
    sbyte DirX, sbyte DirY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense);

public sealed class PlayerLifecycle(World world, GameResources resources, PlayerIndex playerIndex)
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
    
    public Entity CreatePlayer(PlayerTemplate template, Position position, sbyte floor, int mapId, int networkId)
    {
        var entity = world.Create(PlayerArchetype);
        
        var components = new object[]
        {
            new NetworkId { Value = networkId },
            new PlayerId { Value = template.PlayerId },
            new MapId { Value = mapId },
            new NameHandle { Value = resources.Strings.Register(template.Name) },
            new GenderId { Value = template.GenderId },
            new VocationId { Value = template.VocationId },
            new Position { X = position.X, Y = position.Y },
            new Floor { Level = floor },
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
        playerIndex.AddMapping(networkId, entity);
        return entity;
    }
    
    public void DestroyPlayer(Entity entity)
    {
        if (world.Has<NameHandle>(entity))
        {
            var nameRef = world.Get<NameHandle>(entity);
            resources.Strings.Unregister(nameRef.Value);
        }
        playerIndex.RemoveByEntity(entity);
        world.Destroy(entity);
    }
}