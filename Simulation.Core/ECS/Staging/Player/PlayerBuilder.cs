using Arch.Core;
using Simulation.Core.ECS.Components;

namespace Simulation.Core.ECS.Staging.Player;

public static class PlayerBuilder
{
    public static PlayerData BuildPlayerData(this World world, Entity e)
    {
        ref var id = ref world.Get<PlayerId>(e);
        ref var playerInfo = ref world.Get<PlayerInfo>(e);
        ref var pos = ref world.Get<Position>(e);
        ref var dir = ref world.Get<Direction>(e);
        ref var attack = ref world.Get<AttackStats>(e);
        ref var move = ref world.Get<MoveStats>(e);
        ref var health = ref world.Get<Health>(e);

        return new PlayerData
        {
            Id = id.Value,
            Name = playerInfo.Name,
            Gender = playerInfo.Gender,
            Vocation = playerInfo.Vocation,
            PosX = pos.X,
            PosY = pos.Y,
            DirX = dir.X,
            DirY = dir.Y,
            HealthCurrent = health.Current,
            HealthMax = health.Max,
            MoveSpeed = move.Speed,
            AttackCastTime = attack.CastTime,
            AttackCooldown = attack.Cooldown,
            AttackDamage = attack.Damage,
            AttackRange = attack.AttackRange
        };
    }
}