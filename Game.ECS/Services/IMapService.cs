namespace Game.ECS.Services;

public interface IMapService
{
    IMapGrid GetMapGrid(int mapId);
    IMapSpatial GetMapSpatial(int mapId);
    void RegisterMap(int mapId, IMapGrid mapGrid, IMapSpatial mapSpatial);
    void UnregisterMap(int mapId);
    bool HasMap(int mapId);
}