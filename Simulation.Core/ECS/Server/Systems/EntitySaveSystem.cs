using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Server.Staging;
using Simulation.Core.ECS.Shared;
using Simulation.Core.ECS.Shared.Builders;

namespace Simulation.Core.ECS.Server.Systems;

public sealed partial class EntitySaveSystem(World world, IPlayerStagingArea playerStagingArea) : BaseSystem<World, float>(world)
{
    [Query]
    [All<NeedSave, PlayerId, MapId>]
    private void SavePlayer(in Entity entity)
    {
        playerStagingArea.StageSave(World.BuildPlayerData(entity));
    }
}