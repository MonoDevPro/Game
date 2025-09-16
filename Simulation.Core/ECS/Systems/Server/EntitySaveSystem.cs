using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.ECS.Staging;
using Simulation.Core.ECS.Staging.Map;
using Simulation.Core.ECS.Staging.Player;

namespace Simulation.Core.ECS.Systems.Server;

[PipelineSystem(SystemStage.Save)]
[DependsOn(typeof(SpatialIndexSystem))]
public sealed partial class EntitySaveSystem(World world, IWorldStaging staging) : BaseSystem<World, float>(world)
{
    [Query]
    [All<NeedSave, PlayerId, MapId>]
    private void SavePlayer(in Entity entity)
    {
        staging.StageSave(World.BuildPlayerData(entity));
    }

    [Query]
    [All<NeedSave, MapId, MapInfo>]
    private void SaveMap(in Entity entity)
    {
        staging.StageSave(World.BuildMapData(entity));
    }
}