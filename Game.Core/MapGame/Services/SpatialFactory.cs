using Game.ECS.Services;

namespace Game.Core.MapGame.Services;

/// Fábrica para construir ISpatialService a partir de um GameMapService
/// com limites perfeitamente alinhados.
public static class SpatialFactory
{
    public static ISpatialService FromMapBounds(GameMapService map)
    {
        // Bounds alinhados com o mapa (minX=0,minY=0)
        return new QuadTreeSpatialService(0, 0, map.Width, map.Height);
    }

    public static ISpatialService FromCustomBounds(int minX, int minY, int width, int height)
    {
        return new QuadTreeSpatialService(minX, minY, width, height);
    }
}