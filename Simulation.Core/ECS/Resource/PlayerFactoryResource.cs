using Arch.Core;
using Arch.LowLevel;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Components.Data;

namespace Simulation.Core.ECS.Resource;

public sealed class PlayerFactoryResource(World world, PlayerIndexResource playerIndex, SpatialIndexResource spatialIndex, PlayerSaveResource saveResource)
{
    private readonly Resources<string> _playerNames = new();
    
    public bool TryCreatePlayer(in PlayerData data)
    {
        if (playerIndex.TryGetPlayerEntity(data.Id, out var entity))
            TryDestroyPlayer(data.Id);
        
        return CreatePlayerEntity(data);
    }
    
    public bool TryDestroyPlayer(int playerId)
    {
        if (!playerIndex.TryGetPlayerEntity(playerId, out var entity))
            return false; // Jogador não encontrado

        return DestroyPlayerEntity(entity);
    }
    
    public bool CreatePlayerEntity(in PlayerData playerData)
    {
        var nameHandler = _playerNames.Add(playerData.Name);
        
        var e = world.Create(
            new PlayerId { Value = playerData.Id },
            new PlayerInfo { Name = nameHandler, Gender = playerData.Gender, Vocation = playerData.Vocation },
            new Position { X = playerData.PosX, Y = playerData.PosY },
            new Direction { X = playerData.DirX, Y = playerData.DirY },
            new AttackStats { CastTime = playerData.AttackCastTime, Cooldown = playerData.AttackCooldown, Damage = playerData.AttackDamage, AttackRange = playerData.AttackRange },
            new MoveStats { Speed = playerData.MoveSpeed },
            new Health { Current = playerData.HealthCurrent, Max = playerData.HealthMax },
            new State { Value = StateFlags.Idle }
        );
        
        playerIndex.Index(playerData.Id, e);
        spatialIndex.Add(e, new Position(playerData.PosX, playerData.PosY));
        return true;
    }

    private bool DestroyPlayerEntity(Entity entity)
    {
        var data = ExtractPlayerData(entity);
        var playerId = data.Id;
        
        _playerNames.Remove(world.Get<PlayerInfo>(entity).Name);
        world.Destroy(entity);
        playerIndex.Unindex(playerId);
        spatialIndex.Remove(entity);
        return true;
    }
    
    private PlayerData ExtractPlayerData(Entity e)
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
            Name = _playerNames.Get(playerInfo.Name),
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