using Arch.Core;
using Simulation.Core.ECS.Components;

namespace Simulation.Core.ECS.Systems.Resources;

public sealed class PlayerFactoryResource(World world, PlayerIndexResource playerIndex, SpatialIndexResource spatialIndex)
{
    public bool TryCreatePlayer(PlayerData data)
    {
        if (playerIndex.TryGetPlayerEntity(data.Id, out var entity))
        {
            world.Destroy(entity);
            playerIndex.Unindex(data.Id);
            spatialIndex.Remove(entity);
        }
        
        entity = CreatePlayerEntity(world, data);
        playerIndex.Index(data.Id, entity);
        spatialIndex.Add(entity, new Position(data.PosX, data.PosY));
        return true;
    }
    
    public bool TryDestroyPlayer(int playerId, out PlayerData data)
    {
        data = default;
        if (!playerIndex.TryGetPlayerEntity(playerId, out var entity))
            return false; // Jogador não encontrado
        
        data = ExtractPlayerData(world, entity);
        world.Destroy(entity);
        playerIndex.Unindex(playerId);
        spatialIndex.Remove(entity);
        return true;
    }
    
    public static Entity CreatePlayerEntity(World world, PlayerData playerData)
    {
        var e = world.Create(
            new PlayerId { Value = playerData.Id },
            new PlayerInfo { Name = playerData.Name, Gender = playerData.Gender, Vocation = playerData.Vocation },
            new Position { X = playerData.PosX, Y = playerData.PosY },
            new Direction { X = playerData.DirX, Y = playerData.DirY },
            new AttackStats { CastTime = playerData.AttackCastTime, Cooldown = playerData.AttackCooldown, Damage = playerData.AttackDamage, AttackRange = playerData.AttackRange },
            new MoveStats { Speed = playerData.MoveSpeed },
            new Health { Current = playerData.HealthCurrent, Max = playerData.HealthMax },
            new State { Value = StateFlags.Idle }
        );
        return e;
    }

    public static PlayerData ExtractPlayerData(World world, Entity e)
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