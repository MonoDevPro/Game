using GameWeb.Application.Maps.Models;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Utils;

namespace Server.Console.Services;

public class MapRepository(IGameAPI mapApi, ILogger<MapRepository> logger)
{
    private readonly Dictionary<int, (MapService map, string etag)> _cache = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<MapService?> GetMapService(int mapId, CancellationToken cancellationToken = default)
    {
        // 1. Check in-memory cache first
        if (_cache.TryGetValue(mapId, out var cached))
        {
            logger.LogDebug("Map {MapId} loaded from memory cache.", mapId);
            return cached.map;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue(mapId, out cached))
                return cached.map;

            // 2. Try to load from disk cache
            var diskCache = await TryLoadFromDiskCache(mapId, cancellationToken);
            if (diskCache.HasValue)
            {
                logger.LogInformation("Map {MapId} loaded from disk cache.", mapId);
                
                // 3. Validate with API using ETag (cheap HEAD request or If-None-Match)
                var isValid = await ValidateCacheWithETag(mapId, diskCache.Value.etag, cancellationToken);
                if (isValid)
                {
                    _cache[mapId] = diskCache.Value;
                    return diskCache.Value.map;
                }
                logger.LogInformation("Disk cache for map {MapId} is stale, downloading fresh copy.", mapId);
            }

            // 4. Download from API (only if cache miss or stale)
            var (mapData, etag) = await mapApi.GetMapBinaryWithMetaAsync(mapId, cancellationToken);
            if (mapData == null)
            {
                logger.LogWarning("Failed to load map {MapId} from API.", mapId);
                return null;
            }

            var mapService = MapService.CreateFromTemplate(mapData);
            
            // 5. Save to disk cache for next startup
            await SaveToDiskCache(mapId, mapData, etag, cancellationToken);
            
            // 6. Store in memory
            _cache[mapId] = (mapService, etag);
            
            logger.LogInformation("Map '{MapName}' (ID: {MapId}) loaded and cached.", mapService.Name, mapId);
            return mapService;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<(MapService map, string etag)?> TryLoadFromDiskCache(int mapId, CancellationToken ct)
    {
        var cacheDir = Path.Combine(AppContext.BaseDirectory, "cache", "maps");
        var dataPath = Path.Combine(cacheDir, $"map-{mapId}.mpack");
        var metaPath = Path.Combine(cacheDir, $"map-{mapId}.meta");

        if (!File.Exists(dataPath) || !File.Exists(metaPath))
            return null;

        try
        {
            var bytes = await File.ReadAllBytesAsync(dataPath, ct);
            var meta = await File.ReadAllTextAsync(metaPath, ct);
            var etag = meta.Trim();

            var mapData = MemoryPack.MemoryPackSerializer.Deserialize<MapDto>(bytes);
            if (mapData == null) return null;

            var mapService = MapService.CreateFromTemplate(mapData);
            return (mapService, etag);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load map {MapId} from disk cache.", mapId);
            return null;
        }
    }

    private async Task SaveToDiskCache(int mapId, MapDto mapData, string etag, CancellationToken ct)
    {
        var cacheDir = Path.Combine(AppContext.BaseDirectory, "cache", "maps");
        Directory.CreateDirectory(cacheDir);

        var dataPath = Path.Combine(cacheDir, $"map-{mapId}.mpack");
        var metaPath = Path.Combine(cacheDir, $"map-{mapId}.meta");

        try
        {
            var bytes = MemoryPack.MemoryPackSerializer.Serialize(mapData);
            await File.WriteAllBytesAsync(dataPath, bytes, ct);
            await File.WriteAllTextAsync(metaPath, etag, ct);
            
            logger.LogDebug("Map {MapId} saved to disk cache.", mapId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save map {MapId} to disk cache.", mapId);
        }
    }

    private async Task<bool> ValidateCacheWithETag(int mapId, string cachedETag, CancellationToken ct)
    {
        try
        {
            var currentETag = await mapApi.GetMapETagAsync(mapId, ct);
            return currentETag == cachedETag;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to validate cache for map {MapId}, assuming stale.", mapId);
            return false; // On error, assume stale and re-download
        }
    }

    // Pre-warm cache on startup
    public async Task PreloadMapsAsync(int[] mapIds, CancellationToken ct = default)
    {
        logger.LogInformation("Preloading {Count} maps...", mapIds.Length);
        
        var tasks = mapIds.Select(id => GetMapService(id, ct));
        await Task.WhenAll(tasks);
        
        logger.LogInformation("Preloading complete. {Count} maps in cache.", _cache.Count);
    }
}
