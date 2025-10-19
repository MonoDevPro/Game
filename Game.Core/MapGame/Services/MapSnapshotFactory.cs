using Game.Domain.Enums;
using Game.ECS.Components;

namespace Game.Core.MapGame.Services;

public static class MapSnapshotFactory
{
    /// <summary>
    /// Cria um GameMapService a partir de um MapSnapshot.
    /// - Usa o formato row-major do snapshot (Width x Height).
    /// - Converte TileData (byte) para TileType por célula.
    /// - Respeita o modo de armazenamento (padded/compact) do GameMapService.
    /// - Reforça bordas bloqueadas se borderBlocked=true.
    /// </summary>
    /// <param name="snap">Snapshot em row-major (index = y * Width + x)</param>
    /// <param name="id">Id do mapa (definido pelo chamador)</param>
    /// <param name="usePadded">Se verdadeiro, aloca armazenamento padded (potência de 2) em Morton</param>
    /// <param name="borderBlocked">Se verdadeiro, reforça bordas bloqueadas após popular</param>
    public static GameMapService ToMapService(this MapSnapshot snap, int id, bool usePadded = false, bool borderBlocked = true)
    {
        if (snap.Width <= 0 || snap.Height <= 0)
            throw new ArgumentException($"Invalid map dimensions: {snap.Width}x{snap.Height}");

        int expected = snap.Width * snap.Height;
        if (snap.TileData is null || snap.TileData.Length != expected)
            throw new ArgumentException($"TileData length mismatch. Expected {expected}, got {snap.TileData?.Length ?? 0}");
        if (snap.CollisionData is null || snap.CollisionData.Length != expected)
            throw new ArgumentException($"CollisionData length mismatch. Expected {expected}, got {snap.CollisionData?.Length ?? 0}");

        var map = new GameMapService(id, snap.Name ?? string.Empty, snap.Width, snap.Height, usePadded, borderBlocked);

        // Popular armazenamento interno respeitando Morton (padded/compact) via StorageIndex
        int i = 0;
        for (int y = 0; y < snap.Height; y++)
        {
            for (int x = 0; x < snap.Width; x++, i++)
            {
                int dst = map.StorageIndex(x, y);
                map.Tiles[dst] = (TileType)snap.TileData[i];
                map.CollisionMask[dst] = snap.CollisionData[i];
            }
        }

        // Reforça bordas bloqueadas se solicitado
        if (map.BorderBlocked)
        {
            int w = map.Width;
            int h = map.Height;

            for (int x = 0; x < w; x++)
            {
                map.CollisionMask[map.StorageIndex(x, 0)] = 1;
                map.CollisionMask[map.StorageIndex(x, h - 1)] = 1;
            }
            for (int y = 0; y < h; y++)
            {
                map.CollisionMask[map.StorageIndex(0, y)] = 1;
                map.CollisionMask[map.StorageIndex(w - 1, y)] = 1;
            }
        }

        return map;
    }

    /// <summary>
    /// Aplica um snapshot a um GameMapService existente (mesmas dimensões).
    /// Mantém o modo de armazenamento atual (padded/compact).
    /// </summary>
    public static void ApplySnapshot(this GameMapService map, in MapSnapshot snap, bool reapplyBorderBlocked = true)
    {
        if (snap.Width != map.Width || snap.Height != map.Height)
            throw new ArgumentException($"Snapshot dimensions {snap.Width}x{snap.Height} mismatch map {map.Width}x{map.Height}");

        int expected = map.Width * map.Height;
        if (snap.TileData is null || snap.TileData.Length != expected)
            throw new ArgumentException($"TileData length mismatch. Expected {expected}, got {snap.TileData?.Length ?? 0}");
        if (snap.CollisionData is null || snap.CollisionData.Length != expected)
            throw new ArgumentException($"CollisionData length mismatch. Expected {expected}, got {snap.CollisionData?.Length ?? 0}");

        int i = 0;
        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++, i++)
            {
                int dst = map.StorageIndex(x, y);
                map.Tiles[dst] = (TileType)snap.TileData[i];
                map.CollisionMask[dst] = snap.CollisionData[i];
            }
        }

        if (reapplyBorderBlocked && map.BorderBlocked)
        {
            int w = map.Width;
            int h = map.Height;

            for (int x = 0; x < w; x++)
            {
                map.CollisionMask[map.StorageIndex(x, 0)] = 1;
                map.CollisionMask[map.StorageIndex(x, h - 1)] = 1;
            }
            for (int y = 0; y < h; y++)
            {
                map.CollisionMask[map.StorageIndex(0, y)] = 1;
                map.CollisionMask[map.StorageIndex(w - 1, y)] = 1;
            }
        }
    }
}