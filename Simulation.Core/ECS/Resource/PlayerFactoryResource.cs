using Arch.Core;
using Arch.LowLevel;
using GameWeb.Application.Players.Models;
using Simulation.Core.ECS.Components;

namespace Simulation.Core.ECS.Resource;

public sealed class PlayerFactoryResource(World world, PlayerIndexResource playerIndex, SpatialIndexResource spatialIndex)
{
    private readonly Resources<string> _playerNames = new();
    
    public bool TryCreatePlayer(PlayerDto dto, out Entity e)
    {
        PlayerDto playerDto = dto;
        
        if (playerIndex.TryGetPlayerEntity(dto.Id, out var entity))
        {
            if (TryDestroyPlayer(dto.Id, out var existData) && existData is not null) // Remove jogador existente com o mesmo ID
                playerDto = existData; // Mantém os dados do jogador existente
        }
        
        e = CreatePlayerEntity(playerDto);
        return true;
    }
    
    public bool TryDestroyPlayer(int playerId, out PlayerDto? data)
    {
        data = default;
        if (!playerIndex.TryGetPlayerEntity(playerId, out var entity))
            return false; // Jogador não encontrado
        
        data = DestroyPlayerEntity(entity);
        return true;
    }
    
    public Entity CreatePlayerEntity(PlayerDto playerDto)
    {
        var nameHandler = _playerNames.Add(playerDto.Name);
        
        var e = world.Create(
            new PlayerId { Value = playerDto.Id },
            new PlayerName { Value = nameHandler },
            new PlayerGender { Value = playerDto.Gender },
            new PlayerVocation { Value = playerDto.Vocation },
            new Position { X = playerDto.PosX, Y = playerDto.PosY },
            new Direction { X = playerDto.DirX, Y = playerDto.DirY },
            new AttackStats { CastTime = playerDto.AttackCastTime, Cooldown = playerDto.AttackCooldown, Damage = playerDto.AttackDamage, AttackRange = playerDto.AttackRange },
            new MoveStats { Speed = playerDto.MoveSpeed },
            new Health { Current = playerDto.HealthCurrent, Max = playerDto.HealthMax },
            new PlayerState { Flags = StateFlags.Idle }
        );
        
        playerIndex.Index(playerDto.Id, e);
        spatialIndex.Add(e, new Position(playerDto.PosX, playerDto.PosY));
        return e;
    }

    private PlayerDto DestroyPlayerEntity(Entity entity)
    {
        var data = ExtractPlayerData(entity);
        var playerId = data.Id;
        
        _playerNames.Remove(world.Get<PlayerName>(entity).Value);
        world.Destroy(entity);
        playerIndex.Unindex(playerId);
        spatialIndex.Remove(entity);
        return data;
    }
    
    private PlayerDto ExtractPlayerData(Entity e)
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

        return new PlayerDto
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