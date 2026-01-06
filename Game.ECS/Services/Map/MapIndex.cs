using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Navigation.Core.Contracts;

namespace Game.ECS.Services.Map;

/// <summary>
/// Implementação de IMapService para gerenciar múltiplos mapas do jogo.
/// </summary>
public class MapIndex : IMapIndex
{
    private readonly Dictionary<int, IMapGrid> _grids = [];
    private readonly Dictionary<int, IMapSpatial> _spatials = [];

    public IMapGrid GetMapGrid(int mapId) => _grids.TryGetValue(mapId, out var grid) 
        ? grid : throw new KeyNotFoundException($"Mapa com ID {mapId} não encontrado");

    public IMapSpatial GetMapSpatial(int mapId) => _spatials.TryGetValue(mapId, out var spatial) 
        ? spatial : throw new KeyNotFoundException($"Mapa com ID {mapId} não encontrado");

    /// <summary>
    /// Registra um novo mapa no serviço.
    /// </summary>
    public void RegisterMap(int mapId, IMapGrid grid, IMapSpatial spatial)
    {
        _grids[mapId] = grid;
        _spatials[mapId] = spatial;
    }
    
    // Se preferir, exponha um helper para criar map default com layers:
    public void RegisterMap(int mapId, int width, int height, int layers = 1)
    {
        _grids[mapId] = new MapGrid(width, height, layers);
        _spatials[mapId] = new MapSpatial();
    }

    /// <summary>
    /// Remove um mapa do serviço.
    /// </summary>
    public void UnregisterMap(int mapId)
    {
        _grids.Remove(mapId);
        _spatials.Remove(mapId);
    }

    /// <summary>
    /// Obtém todos os IDs de mapas registrados.
    /// </summary>
    public IEnumerable<int> GetRegisteredMapIds() => _grids.Keys;

    /// <summary>
    /// Verifica se um mapa está registrado.
    /// </summary>
    public bool HasMap(int mapId) => _grids.ContainsKey(mapId);

    public MovementResult ValidateMove(int mapId, Position targetPos, Entity movingEntity)
    {
        if (!HasMap(mapId))
            return MovementResult.OutOfBounds;

        var grid = GetMapGrid(mapId);
        var spatial = GetMapSpatial(mapId);

        if (!grid.InBounds(targetPos))
            return MovementResult.OutOfBounds;

        if (grid.IsBlocked(targetPos))
            return MovementResult.BlockedByMap;

        if (spatial.TryGetFirstAt(targetPos, out var occupant) 
            && occupant != default && occupant != Entity.Null 
            && occupant != movingEntity)
        {
            return MovementResult.BlockedByEntity;
        }

        return MovementResult.Allowed;
    }
}
