namespace Game.ECS.Services;

public interface IMapService
{
    IMapGrid GetMapGrid(int mapId);
    IMapSpatial GetMapSpatial(int mapId);
}