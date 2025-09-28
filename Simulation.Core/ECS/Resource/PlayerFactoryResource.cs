using Arch.Core;
using Arch.LowLevel;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Components.Data;

namespace Simulation.Core.ECS.Resource;

public sealed class PlayerFactoryResource(World world, PlayerIndexResource playerIndex, SpatialIndexResource spatialIndex)
{
    private readonly Resources<string> _playerNames = new();
    
    public bool TryCreatePlayer(in PlayerData data, out Entity e)
    {
        PlayerData playerData = data;
        
        if (playerIndex.TryGetPlayerEntity(data.Id, out var entity))
        {
            TryDestroyPlayer(data.Id, out var existData); // Remove jogador existente com o mesmo ID
            playerData = existData; // Mantém os dados do jogador existente
        }
        
        e = CreatePlayerEntity(playerData);
        return true;
    }
    
    public bool TryDestroyPlayer(int playerId, out PlayerData data)
    {
        data = default;
        if (!playerIndex.TryGetPlayerEntity(playerId, out var entity))
            return false; // Jogador não encontrado
        
        data = DestroyPlayerEntity(entity);
        return true;
    }
    
    public Entity CreatePlayerEntity(in PlayerData playerData)
    {
        var nameHandler = _playerNames.Add(playerData.Name);
        
        var e = world.Create(
            new PlayerId { Value = playerData.Id },
            new PlayerName { Value = nameHandler },
            new PlayerGender { Value = playerData.Gender },
            new PlayerVocation { Value = playerData.Vocation },
            new Position { X = playerData.PosX, Y = playerData.PosY },
            new Direction { X = playerData.DirX, Y = playerData.DirY },
            new AttackStats { CastTime = playerData.AttackCastTime, Cooldown = playerData.AttackCooldown, Damage = playerData.AttackDamage, AttackRange = playerData.AttackRange },
            new MoveStats { Speed = playerData.MoveSpeed },
            new Health { Current = playerData.HealthCurrent, Max = playerData.HealthMax },
            new PlayerState { Flags = StateFlags.Idle }
        );
        
        playerIndex.Index(playerData.Id, e);
        spatialIndex.Add(e, new Position(playerData.PosX, playerData.PosY));
        return e;
    }

    private PlayerData DestroyPlayerEntity(Entity entity)
    {
        var data = ExtractPlayerData(entity);
        var playerId = data.Id;
        
        _playerNames.Remove(world.Get<PlayerName>(entity).Value);
        world.Destroy(entity);
        playerIndex.Unindex(playerId);
        spatialIndex.Remove(entity);
        return data;
    }
    
    private PlayerData ExtractPlayerData(Entity e)
    {
        ref var id = ref world.Get<PlayerId>(e);
        ref var playerName = ref world.Get<PlayerName>(e);
        ref var playerGender = ref world.Get<PlayerGender>(e);
        ref var playerVocation = ref world.Get<PlayerVocation>(e);
        ref var pos = ref world.Get<Position>(e);
        ref var dir = ref world.Get<Direction>(e);
        ref var attack = ref world.Get<AttackStats>(e);
        ref var move = ref world.Get<MoveStats>(e);
        ref var health = ref world.Get<Health>(e);

        return new PlayerData
        {
            Id = id.Value,
            Name = _playerNames.Get(playerName.Value),
            Gender = playerGender.Value,
            Vocation = playerVocation.Value,
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