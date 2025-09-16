using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Server.Systems.Builders;
using Simulation.Core.ECS.Shared;
using Simulation.Core.ECS.Shared.Data;
using Simulation.Core.ECS.Shared.Staging;
using Simulation.Core.ECS.Pipeline;

namespace Simulation.Core.ECS.Server.Systems;

 [PipelineSystem(SystemStage.Save)]
 [DependsOn(typeof(Simulation.Core.ECS.Server.Systems.SpatialIndexSystem))]
public sealed partial class EntitySaveSystem(World world, IPlayerStagingArea playerStagingArea, IMapStagingArea map) : BaseSystem<World, float>(world)
{
    [Query]
    [All<NeedSave, PlayerId, MapId>]
    private void SavePlayer(in Entity entity)
    {
        playerStagingArea.StageSave(World.BuildPlayerData(entity));
    }

    [Query]
    [All<NeedSave, MapId, MapInfo>]
    private void SaveMap(in Entity entity)
    {
        map.StageSave(World.BuildMapData(entity));
    }
}