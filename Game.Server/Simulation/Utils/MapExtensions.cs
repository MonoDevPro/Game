using Game.Core.MapGame.Services;
using Game.Domain.Entities;
using Game.Domain.Enums;

namespace Game.Server.Simulation.Utils;

public static class MapExtensions
{
    // Factory
    public static GameMapService ToMapService(this Map entity)
    {
        int w = entity.Width;
        int h = entity.Height;

        if (w <= 0 || h <= 0)
            throw new ArgumentException($"Invalid map dimensions in template Id={entity.Id}: width={w}, height={h}");

        var expected = w * h;

        // defensivo: nunca trabalhar com null
        TileType[] tiles = entity.Tiles;
        if (tiles.Length != expected)
        {
            var fallbackTiles = new TileType[expected];
            Array.Fill(fallbackTiles, TileType.Floor);
            tiles = fallbackTiles;
        }

        var collision = entity.CollisionMask ?? [];
        if (collision.Length != expected)
            collision = new byte[expected]; // default = 0 -> no collision

        // cria a instância e popula os arrays internos
        var ms = new GameMapService(entity.Id, entity.Name ?? string.Empty, w, h, entity.UsePadded, entity.BorderBlocked);

        // copia os dados reais para o armazenamento interno (considera padded/compact)
        ms.PopulateFromRowMajor(tiles, collision);

        // reaplica border blocking depois de popular, garantindo consistência
        if (ms.BorderBlocked)
        {
            var coll = ms.CollisionMask;
            int width = ms.Width;
            int height = ms.Height;
            // top/bottom
            for (int x = 0; x < width; x++)
            {
                coll[ms.StorageIndex(x, 0)] = 1;
                coll[ms.StorageIndex(x, height - 1)] = 1;
            }
            // left/right
            for (int y = 0; y < height; y++)
            {
                coll[ms.StorageIndex(0, y)] = 1;
                coll[ms.StorageIndex(width - 1, y)] = 1;
            }
        }

        return ms;
    }
}