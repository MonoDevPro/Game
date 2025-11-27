using Arch.Core;
using Game.Domain.Templates;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.ECS.Entities.Npc;

public sealed class NpcLifecycle(World world, GameResources resources, NpcIndex npcIndex)
{
    /// <summary>
    /// Arquétipo de NPC com IA.
    /// Suporta movimento, combate, pathfinding A*, mas não tem input de jogador.
    /// </summary>
    public static readonly ComponentType[] NpcArchetype =
    [
        Component<NetworkId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<NameHandle>.ComponentType,
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
        Component<AIControlled>.ComponentType,
        Component<NpcPatrol>.ComponentType,
        Component<NpcPath>.ComponentType,
        Component<NpcType>.ComponentType,
        Component<NpcBrain>.ComponentType,
        Component<NavigationAgent>.ComponentType,
    ];
    
    /// <summary>
    /// Cria um NPC com IA controlada e suporte a pathfinding A*.
    /// </summary>
    public Entity CreateNPC(NpcTemplate template, Position pos, int floor, int mapId, int networkId)
    {
        var entity = world.Create(NpcArchetype);
        var components = new object[]
        {
            new NetworkId { Value = networkId },
            new MapId { Value = mapId },
            new NameHandle { Value = resources.Strings.Register(template.Name) },
            new Position { X = pos.X, Y = pos.Y },
            new Floor { Level = (sbyte)floor },
            new Direction { DirectionX = 0, DirectionY = 1 },
            new Velocity { X = 0, Y = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = template.BaseHp, Max = template.BaseHp, RegenerationRate = template.Stats.HpRegen },
            new Mana { Current = template.BaseMp, Max = template.BaseMp, RegenerationRate = template.Stats.MpRegen },
            new Walkable { BaseSpeed = 2f, CurrentModifier = template.Stats.MovementSpeed },
            new CombatStats 
            {
                AttackPower = template.Stats.PhysicalAttack,
                MagicPower = template.Stats.MagicAttack,
                Defense = template.Stats.PhysicalDefense,
                MagicDefense = template.Stats.MagicDefense,
                AttackRange = template.Behavior.AttackRange,
                AttackSpeed = template.Stats.AttackSpeed > 0 ? template.Stats.AttackSpeed : 1f
            },
            new CombatState 
            { 
                AttackCooldownTimer = 0f,
                IsCasting = false,
                CastTimer = 0f
            },
            new Input { },
            new DirtyFlags { },
            new AIControlled { },
            new NpcType { TemplateId = template.Id },
            new NpcBrain { CurrentState = NpcState.Idle, StateTimer = 0f, CurrentTarget = Entity.Null },
            new NavigationAgent { Destination = null, StoppingDistance = 0f, IsPathPending = false },
            new NpcPatrol
            {
                HomePosition = new Position { X = pos.X, Y = pos.Y, },
            },
            NpcPath.CreateDefault(),
        };
        
        world.SetRange(entity, components);
        npcIndex.AddMapping(networkId, entity);
        return entity;
    }
    
    public void DestroyNPC(Entity entity)
    {
        if (world.Has<NameHandle>(entity))
        {
            var nameRef = world.Get<NameHandle>(entity);
            resources.Strings.Unregister(nameRef.Value);
        }
        npcIndex.RemoveByEntity(entity);
        world.Destroy(entity);
    }
}