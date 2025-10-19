using Game.ECS.Components;

namespace Game.Core.MapGame.Services;

public static class GameMapServiceCollisionCacheExtensions
{
    /// <summary>
    /// Constrói um cache de colisão (bitset row-major) a partir deste mapa.
    /// </summary>
    public static MapCollisionCache BuildCollisionCache(this GameMapService map)
        => MapCollisionCache.Build(map);

    /// <summary>
    /// Seta colisão no mapa e reflete no cache (sem exceções em hot path).
    /// </summary>
    public static bool SetBlockedWithCache(this GameMapService map, MapCollisionCache cache, in Position p, bool blocked)
    {
        if (!map.InBounds(p)) return false;
        map.SetBlocked(p, blocked);
        cache.SetBlocked(p, blocked);
        return true;
    }

    /// <summary>
    /// Consulta colisão via cache (fallback no mapa caso fora dos limites).
    /// </summary>
    public static bool TryIsBlockedFast(this GameMapService map, MapCollisionCache cache, in Position p, out bool blocked)
    {
        if (cache.TryIsBlocked(p, out blocked)) return true;
        blocked = !map.InBounds(p) || map.IsBlocked(p);
        return false;
    }

    /// <summary>
    /// Reaplica completamente o estado de colisão do mapa no cache (rebuild).
    /// </summary>
    public static void RebuildCollisionCache(this GameMapService map, MapCollisionCache cache)
        => cache.ApplyFromMap(map);

    /// <summary>
    /// Sincroniza apenas uma célula do mapa para o cache (após alterar CollisionMask diretamente).
    /// </summary>
    public static void SyncCellToCache(this GameMapService map, MapCollisionCache cache, in Position p)
        => cache.ApplyCell(map, p);
}