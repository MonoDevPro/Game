using Arch.Core;
using Simulation.Core.ECS.Shared;
using Simulation.Core.ECS.Shared.Data;

namespace Simulation.Core.ECS.Server.Systems.Builders;

public static class MapBuilder
{
    public static MapData BuildMapData(this World world, Entity e)
    {
        ref var mapId = ref world.Get<MapId>(e);
        ref var mapInfo = ref world.Get<MapInfo>(e);
        return new MapData
        {
            Name = mapInfo.Name, 
            MapId = mapId.Value, 
            Width = mapInfo.Width, 
            Height = mapInfo.Height
        };
    }
}