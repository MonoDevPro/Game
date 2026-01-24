using Arch.Core;
using Game.Infrastructure.ArchECS.Commons.Components;

namespace Game.Infrastructure.ArchECS.Services.Map;

/// <summary>
/// Registro global de mapas do jogo.
/// Substitui MapIndex, MapGrid, MapSpatial, NavigationGridAdapter. 
/// </summary>
public sealed class WorldMapRegistry
{
    private readonly Dictionary<int, WorldMap> _maps = new();
    
    public WorldMap this[int mapId] => _maps. TryGetValue(mapId, out var map) 
        ? map 
        : throw new KeyNotFoundException($"Map {mapId} not found");
    
    public WorldMap Register(int id, string name, int width, int height, int layers = 1)
    {
        var map = new WorldMap(id, name, width, height, layers);
        _maps[id] = map;
        return map;
    }
    
    public void Register(WorldMap map) => _maps[map.Id] = map;
    
    public void RegisterRange(IEnumerable<WorldMap> maps)
    {
        foreach (var map in maps)
        {
            _maps[map.Id] = map;
        }
    }
    
    
    public bool TryGet(int id, out WorldMap?  map) => _maps.TryGetValue(id, out map);
    
    public void Unregister(int id) => _maps.Remove(id);
    
    public bool Contains(int id) => _maps.ContainsKey(id);
    
    public IEnumerable<int> MapIds => _maps. Keys;
    public IEnumerable<WorldMap> Maps => _maps.Values;
    
    // Conveniência:  valida movimento cross-map
    public MoveResult ValidateMove(int mapId, Position target, int targetFloor, Entity entity)
    {
        if (!_maps.TryGetValue(mapId, out var map))
            return MoveResult. OutOfBounds;
        return map.ValidateMove(target, targetFloor, entity);
    }
}