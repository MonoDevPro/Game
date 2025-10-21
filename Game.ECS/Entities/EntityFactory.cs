using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Archetypes;
using Game.ECS.Entities.Data;

namespace Game.ECS.Entities;

public class EntityFactory(World world) : IEntityFactory
{
    public Entity CreatePlayer(in PlayerCharacter data)
    {
        var entity = world.Create(GameArchetypes.PlayerCharacter);
        var components = new object[]
        {
            new NetworkId { Value = data.NetworkId },
            new PlayerId { Value = data.PlayerId },
            new Position { X = data.SpawnX, Y = data.SpawnY, Z = data.SpawnZ },
            new Facing { DirectionX = data.FacingX, DirectionY = data.FacingY },
            new Velocity { DirectionX = 0, DirectionY = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = data.Hp, Max = data.MaxHp, RegenerationRate = data.HpRegen },
            new Mana { Current = data.Mp, Max = data.MaxMp, RegenerationRate = data.MpRegen },
            new Walkable { BaseSpeed = 5f, CurrentModifier = data.MovementSpeed },
            new Attackable { BaseSpeed = 1f, CurrentModifier = data.AttackSpeed },
            new AttackPower { Physical = data.PhysicalAttack, Magical = data.MagicAttack },
            new Defense { Physical = data.PhysicalDefense, Magical = data.MagicDefense },
            new CombatState { },
            new PlayerInput { },
            new PlayerControlled()
        };
        world.SetRange(entity, components);
        return entity;
    }

    public Entity CreateRemotePlayer(in PlayerCharacter data)
    {
        var entity = CreatePlayer(data);
        world.Add<RemotePlayerTag>(entity);
        return entity;
    }

    public Entity CreateLocalPlayer(in PlayerCharacter data)
    {
        var entity = CreatePlayer(data);
        world.Add<LocalPlayerTag>(entity);
        return entity;
    }

    /// <summary>
    /// Cria um NPC com IA controlada.
    /// </summary>
    public Entity CreateNPC(in NPCCharacter data)
    {
        var entity = world.Create(GameArchetypes.NPCCharacter);
        var components = new object[]
        {
            new NetworkId { Value = data.NetworkId },
            new Position { X = data.PositionX, Y = data.PositionY, Z = data.PositionZ },
            new Facing { DirectionX = 0, DirectionY = 0 },
            new Velocity { DirectionX = 0, DirectionY = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = data.Hp, Max = data.MaxHp, RegenerationRate = data.HpRegen },
            new Walkable { BaseSpeed = 3f, CurrentModifier = 1f },
            new AttackPower { Physical = data.PhysicalAttack, Magical = data.MagicAttack },
            new Defense { Physical = data.PhysicalDefense, Magical = data.MagicDefense },
            new CombatState { },
            new AIControlled()
        };
        world.SetRange(entity, components);
        return entity;
    }

    /// <summary>
    /// Cria um projétil (bala, flecha, magia).
    /// </summary>
    public Entity CreateProjectile(in ProjectileData data)
    {
        var entity = world.Create(GameArchetypes.Projectile);
        var components = new object[]
        {
            new NetworkId { Value = data.NetworkId },
            new Position { X = data.StartX, Y = data.StartY, Z = data.StartZ },
            new Facing { DirectionX = data.DirectionX, DirectionY = data.DirectionY },
            new Velocity { DirectionX = data.DirectionX, DirectionY = data.DirectionY, Speed = data.Speed },
            new AttackPower { Physical = data.PhysicalDamage, Magical = data.MagicalDamage },
        };
        world.SetRange(entity, components);
        return entity;
    }

    /// <summary>
    /// Cria um item solto no chão.
    /// </summary>
    public Entity CreateDroppedItem(in DroppedItemData data)
    {
        var entity = world.Create(GameArchetypes.DroppedItem);
        var components = new object[]
        {
            new NetworkId { Value = data.NetworkId },
            new Position { X = data.PositionX, Y = data.PositionY, Z = data.PositionZ },
        };
        world.SetRange(entity, components);
        return entity;
    }
}