using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.ECS.Staging.Map;
using Simulation.Core.ECS.Staging.Player;

namespace Simulation.Core.ECS.Systems;

 [PipelineSystem(SystemStage.Logic, 0)]
public sealed partial class EntityFactorySystem(World world): BaseSystem<World, float>(world)
{
    [Query]
    [All<NewlyCreated, MapData>]
    private void CreateMap(in Entity entity, ref MapData data)
    {
        World.CreateMapEntity(entity, data);
        World.Remove<MapData>(entity);
    }
    
    [Query]
    [All<NewlyCreated, PlayerData>]
    private void CreatePlayer(in Entity entity, ref PlayerData data)
    {
        World.CreatePlayerEntity(entity, data);
        World.Remove<PlayerData>(entity);
    }

}