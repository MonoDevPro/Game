using Simulation.Core.ECS.Indexes.Map;
using Simulation.Core.Persistence.Contracts;
using Simulation.Core.ECS.Data;

namespace Simulation.Core.ECS;

/// <summary>
/// Serviço central para a gestão do mundo do jogo.
/// Contém o índice espacial e os dados do mapa, atuando como a única fonte da verdade.
/// </summary>
public class WorldManager(IMapRepository mapRepository, WorldSpatial worldSpatial)
{
    public WorldSpatial WorldSpatial { get; private set; } = worldSpatial;
    public MapService? MapService { get; private set; }

    /// <summary>
    /// Carrega os dados do mapa a partir da base de dados e inicializa o WorldSpatial.
    /// </summary>
    public async Task InitializeAsync(int mapIdToLoad)
    {
        var mapData = await mapRepository.GetMapAsync(mapIdToLoad);
        if (mapData == null)
        {
            throw new Exception($"Mapa com ID {mapIdToLoad} não encontrado na base de dados.");
        }

        MapService = MapService.CreateFromTemplate(mapData.Value);
        MapService.PopulateFromRowMajor(mapData.Value.TilesRowMajor, mapData.Value.CollisionRowMajor);
        
        WorldSpatial = new WorldSpatial(0, 0, MapService.Width, MapService.Height);
    }

    /// <summary>
    /// Guarda o estado atual do mapa na base de dados.
    /// </summary>
    public async Task SaveMapAsync()
    {
        if (MapService == null)
            throw new InvalidOperationException("MapService não está inicializado. Chame InitializeAsync primeiro.");
        
        var mapData = new MapData
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
        
        await mapRepository.AddFromDataAsync(mapData);
    }
}