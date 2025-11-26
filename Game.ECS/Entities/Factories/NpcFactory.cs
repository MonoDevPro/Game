using Arch.Core;
using Game.Domain.Templates;
using Game.ECS.Components;
using Game.ECS.Entities.Archetypes;
using Game.ECS.Entities.Data;

namespace Game.ECS.Entities.Factories;

public static partial class EntityFactory
{
    /// <summary>
    /// Cria um NPC com IA controlada e suporte a pathfinding A*.
    /// </summary>
    public static Entity CreateNPC(this World world, NpcTemplate template, Position pos, int floor, int mapId, int networkId)
    {
        var entity = world.Create(GameArchetypes.NPCCharacter);
        var components = new object[]
        {
            new NetworkId { Value = networkId },
            new MapId { Value = mapId },
            new Position { X = pos.X, Y = pos.Y },
            new Floor { Level = (sbyte)floor },
            new Facing { DirectionX = 0, DirectionY = 1 },
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
            new NpcInfo { GenderId = (byte)template.Gender, VocationId = (byte)template.Vocation },
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
        return entity;
    }
    
    public static Entity CreateNPC(this World world, in NPCData data)
    {
        var entity = world.Create(GameArchetypes.NPCCharacter);
        var components = new object[]
        {
            new NetworkId { Value = data.NetworkId },
            new MapId { Value = data.MapId },
            new Position { X = data.PositionX, Y = data.PositionY },
            new Floor { Level = data.Floor },
            new Facing { DirectionX = 0, DirectionY = 1 },
            new Velocity { X = 0, Y = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = data.Hp, Max = data.MaxHp, RegenerationRate = data.HpRegen },
            new Mana { Current = data.Mp, Max = data.MaxMp, RegenerationRate = data.MpRegen },
            new Walkable { BaseSpeed = 2f, CurrentModifier = data.MovementSpeed },
            new CombatStats 
            { 
                AttackPower = data.PhysicalAttack,
                MagicPower = data.MagicAttack,
                Defense = data.PhysicalDefense,
                MagicDefense = data.MagicDefense,
                AttackRange = 1.5f,
                AttackSpeed = data.AttackSpeed > 0 ? data.AttackSpeed : 1f
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
            new NpcInfo { GenderId = data.Gender, VocationId = data.Vocation },
            new NpcType { TemplateId = "" },
            new NpcBrain { CurrentState = NpcState.Idle, StateTimer = 0f, CurrentTarget = Entity.Null },
            new NavigationAgent { Destination = null, StoppingDistance = 0f, IsPathPending = false },
            new NpcPatrol
            {
                HomePosition = new Position { X = data.PositionX, Y = data.PositionY, },
            },
            NpcPath.CreateDefault(),
        };
        world.SetRange(entity, components);
        return entity;
    }
}