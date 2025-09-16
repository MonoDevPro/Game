using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Pipeline;

namespace Simulation.Core.ECS.Systems;

 [PipelineSystem(SystemStage.Destruction)]
public sealed partial class EntityDestructorSystem(World world): BaseSystem<World, float>(world)
{
    [Query]
    [All<NewlyDestroyed>]
    private void DestroyEntity(in Entity entity)
    {
        World.Destroy(entity);
    }
}