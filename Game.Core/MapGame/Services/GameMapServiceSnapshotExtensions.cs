using Game.ECS.Components;

namespace Game.Core.MapGame.Services;

public static class GameMapServiceSnapshotExtensions
{
    /// <summary>
    /// Cria um snapshot em formato row-major (Width x Height).
    /// - TileData: byte por célula (conversão direta do enum TileType para byte).
    /// - CollisionData: byte por célula (0 livre, 1 bloqueado).
    /// </summary>
    public static MapSnapshot CreateSnapshot(this GameMapService map)
    {
        int w = map.Width;
        int h = map.Height;
        int n = w * h;

        var tilesRowMajor = new byte[n];
        var collisionRowMajor = new byte[n];

        int i = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++, i++)
            {
                int storageIdx = map.StorageIndex(x, y);
                tilesRowMajor[i] = (byte)map.Tiles[storageIdx];
                collisionRowMajor[i] = map.CollisionMask[storageIdx];
            }
        }

        return new MapSnapshot(
            map.Name ?? string.Empty,
            w,
            h,
            tilesRowMajor,
            collisionRowMajor
        );
    }

    /// <summary>
    /// Constrói um snapshot apenas de uma sub-área (inclusiva) do mapa, em row-major.
    /// Útil para streaming de regiões.
    /// </summary>
    public static MapSnapshot CreateSnapshotArea(this GameMapService map, int minX, int minY, int maxX, int maxY, string? nameOverride = null)
    {
        // Clampa aos limites do mapa
        minX = minX < 0 ? 0 : (minX >= map.Width ? map.Width - 1 : minX);
        minY = minY < 0 ? 0 : (minY >= map.Height ? map.Height - 1 : minY);
        maxX = maxX < 0 ? 0 : (maxX >= map.Width ? map.Width - 1 : maxX);
        maxY = maxY < 0 ? 0 : (maxY >= map.Height ? map.Height - 1 : maxY);

        if (maxX < minX) (minX, maxX) = (maxX, minX);
        if (maxY < minY) (minY, maxY) = (maxY, minY);

        int w = maxX - minX + 1;
        int h = maxY - minY + 1;
        int n = w * h;

        var tilesRowMajor = new byte[n];
        var collisionRowMajor = new byte[n];

        int i = 0;
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++, i++)
            {
                int storageIdx = map.StorageIndex(x, y);
                tilesRowMajor[i] = (byte)map.Tiles[storageIdx];
                collisionRowMajor[i] = map.CollisionMask[storageIdx];
            }
        }

        return new MapSnapshot(
            nameOverride ?? map.Name ?? string.Empty,
            w,
            h,
            tilesRowMajor,
            collisionRowMajor
        );
    }
}