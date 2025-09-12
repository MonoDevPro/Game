using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Shared;

namespace Simulation.Core.ECS.Server.Systems;

public sealed partial class EntityDestructorSystem(World world): BaseSystem<World, float>(world)
{
    [Query]
    [All<NewlyDestroyed>]
    private void DestroyEntity(in Entity entity)
    {
        World.Destroy(entity);
    }
}