using Arch.Core;
using Simulation.Core.ECS.Shared;
using Simulation.Core.ECS.Shared.Data;

namespace Simulation.Core.ECS.Server.Systems.Factories;

public static class MapFactory
{
    public static Entity CreateMapEntity(this World world, Entity entity, MapData mapData)
    {
        world.Add(entity,
            new MapId { Value = mapData.MapId },
            new MapInfo { Name = mapData.Name, Width = mapData.Width, Height = mapData.Height });
        return entity;
    }
}