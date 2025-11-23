using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Archetypes;
using Game.ECS.Entities.Data;
using Game.ECS.Entities.Repositories;

namespace Game.ECS.Entities.Factories;

public static partial class EntityFactory
{
    public static Entity CreatePlayer(this World world, PlayerIndex? index, in PlayerData data)
    {
        var entity = world.Create(GameArchetypes.PlayerCharacter);
        var components = new object[]
        {
            new NetworkId { Value = data.NetworkId },
            new MapId { Value = data.MapId },
            new PlayerInfo { GenderId = data.Gender, VocationId = data.Vocation},
            new Position { X = data.PosX, Y = data.PosY },
            new Floor { Level = data.Floor },
            new Facing { DirectionX = data.FacingX, DirectionY = data.FacingY },
            new Velocity { X = 0, Y = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = data.Hp, Max = data.MaxHp, RegenerationRate = data.HpRegen },
            new Mana { Current = data.Mp, Max = data.MaxMp, RegenerationRate = data.MpRegen },
            new Walkable { BaseSpeed = 3f, CurrentModifier = data.MovementSpeed },
            new Attackable { BaseSpeed = 1f, CurrentModifier = data.AttackSpeed },
            new AttackPower { Physical = data.PhysicalAttack, Magical = data.MagicAttack },
            new Defense { Physical = data.PhysicalDefense, Magical = data.MagicDefense },
            new CombatState { InCombat = false, TimeSinceLastHit = SimulationConfig.HealthRegenDelayAfterCombat },
            new Input { },
            new DirtyFlags(),
            new PlayerControlled(),
            new PlayerId { Value = data.PlayerId },
        };
        world.SetRange(entity, components);
        index?.AddMapping(data.NetworkId, entity);
        return entity;
    }
    
    public static bool TryDestroyPlayer(this World world, PlayerIndex index, int networkId)
    {
        if (!index.TryGetEntity(networkId, out var entity))
            return false;
        index.RemoveByEntity(entity);
        world.Destroy(entity);
        return true;
    }
}