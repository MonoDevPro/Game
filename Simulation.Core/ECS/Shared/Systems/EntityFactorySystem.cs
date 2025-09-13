using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Shared.Data;
using Simulation.Core.ECS.Shared.Systems.Factories;

namespace Simulation.Core.ECS.Shared.Systems;

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