using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Indexes.Map;
using Simulation.Core.Persistence.Contracts;

namespace Simulation.Core.ECS;

/// <summary>
/// Serviço central para a gestão do mundo do jogo.
/// Contém o índice espacial e os dados do mapa, atuando como a única fonte da verdade.
/// </summary>
public class WorldManager
{
    public WorldSpatial WorldSpatial { get; private set; }
    public MapService MapService { get; private set; }
    
    /// <summary>
    /// Serviço central para a gestão do mundo do jogo.
    /// Contém o índice espacial e os dados do mapa, atuando como a única fonte da verdade.
    /// </summary>
    public WorldManager(MapService mapService)
    {
        WorldSpatial = new WorldSpatial(
            minX: 0, 
            minY: 0, 
            width: mapService.Width, 
            height: mapService.Height);
        MapService = mapService;
    }

    public MapData GetMapData()
    {
        return new MapData
        {
            Id = MapService.Id,
            Name = MapService.Name,
            Width = MapService.Width,
            Height = MapService.Height,
            TilesRowMajor = MapService.Tiles,
            CollisionRowMajor = MapService.CollisionMask,
            UsePadded = MapService.UsePadded,
            BorderBlocked = MapService.BorderBlocked
        };
    }
}