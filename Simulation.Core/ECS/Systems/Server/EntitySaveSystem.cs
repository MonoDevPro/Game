using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.Ports;

namespace Simulation.Core.ECS.Systems.Server;

[PipelineSystem(SystemStage.Save)]
[DependsOn(typeof(SpatialIndexSystem))]
public sealed partial class EntitySaveSystem(World world, IWorldSaver saver) : BaseSystem<World, float>(world)
{
    [Query]
    [All<NeedSave, PlayerId>]
    private void SavePlayer(in Entity entity)
    {
        World.Remove<NeedSave>(entity);
        saver.StageSave(EntityFactorySystem.ExtractPlayerData(World, entity)); 
    }
}