using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Utils;
using Simulation.Core.Server.API;

namespace Simulation.Core.Server.Map;

public sealed record MapCacheEntry(MapService Map, string ETag);

public sealed class MapRepository
{
    private readonly IMemoryCache _memoryCache;
    private readonly MapServiceFactory _factory;
    private readonly IGameAPI _mapApi;
    private readonly ILogger<MapRepository> _logger;

    public MapRepository(
        IMemoryCache memoryCache,
        MapServiceFactory factory,
        IGameAPI mapApi,
        ILogger<MapRepository> logger)
    {
        _memoryCache = memoryCache;
        _factory = factory;
        _mapApi = mapApi;
        _logger = logger;
    }

    public async Task<MapService?> GetMapService(int mapId, CancellationToken ct = default)
    {
        var key = $"map:{mapId}";

        // Try fast path: if present and valid, return
        if (_memoryCache.TryGetValue<MapCacheEntry>(key, out var existing) && existing is not null)
        {
            _logger.LogDebug("Map {MapId} loaded from memory cache.", mapId);
            return existing.Map;
        }

        // GetOrCreateAsync ensures single factory invocation for the same key
        var entry = await _memoryCache.GetOrCreateAsync(key, async cacheEntry =>
        {
            // configure eviction/size/expiration as needed
            cacheEntry.SetPriority(CacheItemPriority.High);
            // if you set global SizeLimit, set per-entry Size
            cacheEntry.SetSize(1);
            cacheEntry.SlidingExpiration = TimeSpan.FromMinutes(30);

            // 1) Try disk cache
            var disk = await _factory.TryLoadFromDiskCacheAsync(mapId, ct);
            if (disk is not null)
            {
                _logger.LogInformation("Map {MapId} loaded from disk cache.", mapId);

                // 2) validate ETag with API
                try
                {
                    var currentEtag = await _mapApi.GetMapETagAsync(mapId, ct);
                    if (currentEtag == disk.Value.etag)
                    {
                        return new MapCacheEntry(disk.Value.map, disk.Value.etag);
                    }
                    _logger.LogInformation("Disk cache for map {MapId} is stale.", mapId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ETag validation failed for map {MapId}; re-downloading.", mapId);
                    // fall through to download
                }
            }

            // 3) Download from API
            var (mapDto, etag) = await _mapApi.GetMapBinaryWithMetaAsync(mapId, ct);
            if (mapDto == null)
            {
                _logger.LogWarning("Failed to download map {MapId} from API.", mapId);
                // return a failed sentinel? We remove the created empty cache (avoid storing null)
                // Throw to avoid caching a null result; caller handles null
                throw new InvalidOperationException($"Failed to download map {mapId}");
            }

            // create MapService, persist to disk, return
            var mapService = _factory.CreateFromDto(mapDto);
            await _factory.SaveToDiskCacheAsync(mapId, mapDto, etag, ct);

            return new MapCacheEntry(mapService, etag ?? string.Empty);
        });

        // If GetOrCreateAsync factory threw, we should catch above; otherwise entry is set
        return entry?.Map;
    }
    
    public void RemoveFromMemory(int mapId) => _memoryCache.Remove($"map:{mapId}");
}