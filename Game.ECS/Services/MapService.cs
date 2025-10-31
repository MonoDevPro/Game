namespace Game.ECS.Services;

/// <summary>
/// Implementação de IMapService para gerenciar múltiplos mapas do jogo.
/// </summary>
public class MapService : IMapService
{
    private readonly Dictionary<int, IMapGrid> _grids = [];
    private readonly Dictionary<int, IMapSpatial> _spatials = [];

    public MapService()
    {
        // Inicializa com um mapa padrão (ID 0)
        RegisterMap(0, new MapGrid(100, 100), new MapSpatial());
    }

    public IMapGrid GetMapGrid(int mapId) => 
        _grids.TryGetValue(mapId, out var grid) 
        ? grid 
        : throw new KeyNotFoundException($"Mapa com ID {mapId} não encontrado");

    public IMapSpatial GetMapSpatial(int mapId)
    {
        return _spatials.TryGetValue(mapId, out var spatial) ? spatial : throw new KeyNotFoundException($"Mapa com ID {mapId} não encontrado");
    }

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
}
