using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.Core.MapGame.Services;

/// Compositor/coordenador do mapa em runtime, garantindo coesão entre:
/// - GameMapService (dados)
/// - ISpatialService (ocupação)
/// - MapCollisionCache (acesso rápido à colisão)
/// Fornece utilitários seguros e prontos para uso pela simulação.
public sealed class MapRuntime
{
    public GameMapService Map { get; }
    public ISpatialService Spatial { get; }
    public MapCollisionCache CollisionCache { get; }

    public MapRuntime(GameMapService map, ISpatialService spatial, MapCollisionCache cache)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        Spatial = spatial ?? throw new ArgumentNullException(nameof(spatial));
        CollisionCache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public static MapRuntime Build(GameMapService map, Func<GameMapService, ISpatialService> spatialFactory)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));
        var spatial = spatialFactory?.Invoke(map) ?? throw new ArgumentNullException(nameof(spatialFactory));
        var cache = map.BuildCollisionCache();
        return new MapRuntime(map, spatial, cache);
    }

    // Conveniência: Begin/End do frame (se o spatial usar snapshot/commit futuramente)
    public void BeginFrame(uint tick) => Spatial.BeginFrame(tick);
    public void EndFrame() => Spatial.EndFrame();

    // Movimentação atômica integrando mapa + spatial + reserva opcional
    public bool TryMoveEntity(in Entity e, in Position from, in Position to, bool useReservation,
        out MapNavigation.MoveBlockReason reason)
    {
        return MapNavigation.TryMoveAtomic(Map, Spatial, e, from, to, useReservation, out reason, out _);
    }

    // Acesso rápido a colisão via cache
    public bool IsBlockedFast(in Position p)
        => CollisionCache.TryIsBlocked(p, out var blocked) && blocked;

    // Mutação consistente de colisão (mapa + cache)
    public bool SetBlocked(in Position p, bool blocked)
        => Map.SetBlockedWithCache(CollisionCache, p, blocked);

    // Spawn/despawn helpers (ocupação spatial)
    public void InsertEntityAt(in Entity e, in Position p)
        => Spatial.Insert(p, e);

    public bool RemoveEntityAt(in Entity e, in Position p)
        => Spatial.Remove(p, e);

    // Queries
    public bool TryGetFirstOccupant(in Position p, out Entity e)
        => Spatial.TryGetFirstAt(p, out e);
}