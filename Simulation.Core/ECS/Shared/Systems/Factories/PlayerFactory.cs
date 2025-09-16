using Arch.Core;
using Simulation.Core.ECS.Shared.Data;
using Simulation.Core.ECS.Shared.Systems.Network;

namespace Simulation.Core.ECS.Shared.Systems.Factories;

public static class PlayerFactory
{
    public static Entity CreatePlayerEntity(this World world, Entity entity, PlayerData playerData)
    {
        world.Add(entity,
            new PlayerId { Value = playerData.Id },
            new PlayerInfo { Name = playerData.Name, Gender = playerData.Gender, Vocation = playerData.Vocation },
            new MapId { Value = playerData.MapId },
            new Position { X = playerData.PosX, Y = playerData.PosY },
            new Direction { X = playerData.DirX, Y = playerData.DirY },
            new AttackStats { CastTime = playerData.AttackCastTime, Cooldown = playerData.AttackCooldown, Damage = playerData.AttackDamage, AttackRange = playerData.AttackRange },
            new MoveStats { Speed = playerData.MoveSpeed },
            new Health { Current = playerData.HealthCurrent, Max = playerData.HealthMax },
            new ActionComponent { Value = StateFlags.Idle }
        );
        return entity;
    }
}