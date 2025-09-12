using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Server.Staging;
using Simulation.Core.ECS.Shared;
using Simulation.Core.Models;
using Simulation.Core.Persistence.Contracts;

namespace Simulation.Core.ECS.Server.Systems;

public sealed partial class EntitySaveSystem(World world, IRepository<int, PlayerModel> memoryRepo, IPlayerStagingArea playerStagingArea) : BaseSystem<World, float>(world)
{
    [Query]
    [All<NeedSave, PlayerId, MapId>]
    private void SavePlayer(in Entity entity, ref PlayerId playerId, ref MapId mapId,
        ref Position position, ref Direction direction,
        ref AttackStats attackStats, ref MoveStats moveStats,
        ref Health health)
    {
        if (!memoryRepo.TryGet(playerId.Value, out var existingData) || existingData is null)
            existingData = new PlayerModel
            {
                Id = playerId.Value
            };
        
        // Atualiza os dados
        existingData.MapId = mapId.Value;
        existingData.PosX = position.X;
        existingData.PosY = position.Y;
        existingData.DirX = direction.X;
        existingData.DirY = direction.Y;
        existingData.AttackCastTime = attackStats.CastTime;
        existingData.AttackCooldown = attackStats.Cooldown;
        existingData.AttackDamage = attackStats.Damage;
        existingData.AttackRange = attackStats.AttackRange;
        existingData.MoveSpeed = moveStats.Speed;
        existingData.HealthCurrent = health.Current;
        existingData.HealthMax = health.Max;
        
        playerStagingArea.StageSave(existingData);
    }
}