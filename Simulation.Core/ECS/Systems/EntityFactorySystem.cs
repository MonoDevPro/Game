using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Data;
using Simulation.Core.ECS.Pipeline;

namespace Simulation.Core.ECS.Systems;

[PipelineSystem(SystemStage.Logic, 0)]
public sealed class EntityFactorySystem(World world, Group<float> container): BaseSystem<World, float>(world)
{
    private readonly IndexSystem _indexSystem = container.Get<IndexSystem>();
    public bool TryCreate(PlayerData data, out Entity? entity)
    {
        if (_indexSystem.TryGetPlayerEntity(data.Id, out _))
        {
            entity = null;
            return false; // Jogador já está online
        }
        
        entity = CreatePlayerEntity(World, data);
        return true;
    }
    
    public bool TryDestroy(int playerId, out PlayerData data)
    {
        data = default;
        if (!_indexSystem.TryGetPlayerEntity(playerId, out var entity))
            return false; // Jogador não encontrado
        
        World.Add<Unindexed, NeedSave, NeedDelete>(entity);
        
        data = ExtractPlayerData(World, entity);
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
            new ActionComponent { Value = StateFlags.Idle }
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

        return new Data.PlayerData
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