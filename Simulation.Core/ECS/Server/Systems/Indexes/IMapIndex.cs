using Arch.Core;
using Simulation.Core.ECS.Shared.Data;
using Simulation.Core.ECS.Shared.Utils.Map;

namespace Simulation.Core.ECS.Server.Systems.Indexes;

/// <summary>
/// Define um contrato para um índice que mapeia um ID de mapa para a sua entidade e serviço.
/// </summary>
public interface IMapIndex
{
    /// <summary>
    /// Obtém a entidade e o serviço de um mapa pelo seu ID, se existir e estiver vivo.
    /// </summary>
    bool TryGetMap(int mapId, out MapInstance mapInstance);
}

/// <summary>
/// Um wrapper de conveniência que une a entidade de um mapa com o seu MapService.
/// Facilita o acesso aos dados e à lógica do mapa.
/// </summary>
public readonly record struct MapInstance(Entity Entity, MapService Service)
{
    // Propriedades delegadas para acesso fácil
    public int MapId => Service.MapId;
    public string Name => Service.Name;
    public int Width => Service.Width;
    public int Height => Service.Height;
    
    public MapData GetMapData()
    {
        return new MapData
        {
            MapId = Service.MapId,
            Name = $"Map_{Service.MapId}",
            Width = Service.Width,
            Height = Service.Height,
            TilesRowMajor = Service.Tiles,
            CollisionRowMajor = Service.CollisionMask,
            UsePadded = Service.UsePadded,
            BorderBlocked = Service.BorderBlocked
        };
    }
}