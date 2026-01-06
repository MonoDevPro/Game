using System.Collections.Concurrent;
using Game.Domain.Entities;
using Game.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace Game.Server.Simulation.Maps;

/// <summary>
/// In-memory cache for Maps loaded from persistence. Avoids duplicated DB hits on concurrent requests.
/// Singleton-friendly: uses IServiceScopeFactory per load.
/// </summary>
public sealed class MapCacheService(IServiceScopeFactory scopeFactory, ILogger<MapCacheService> logger) : IMapCacheService
{
    private readonly ConcurrentDictionary<int, Lazy<Task<Map>>> _cache = new();

    public Task<Map> GetMapAsync(int mapId, CancellationToken cancellationToken = default)
    {
        if (mapId <= 0) throw new ArgumentOutOfRangeException(nameof(mapId));

        var lazy = _cache.GetOrAdd(mapId, id => new Lazy<Task<Map>>(() => LoadMapAsync(id, cancellationToken)));
        return AwaitAndRepairOnFailure(mapId, lazy);
    }

    public Task InvalidateAsync(int mapId)
    {
        _cache.TryRemove(mapId, out _);
        return Task.CompletedTask;
    }

    public Task InvalidateAllAsync()
    {
        _cache.Clear();
        return Task.CompletedTask;
    }

    private async Task<Map> AwaitAndRepairOnFailure(int mapId, Lazy<Task<Map>> lazy)
    {
        try
        {
            return await lazy.Value.ConfigureAwait(false);
        }
        catch
        {
            // If loading failed, remove the cached entry so next request can retry.
            _cache.TryRemove(mapId, out _);
            throw;
        }
    }

    private async Task<Map> LoadMapAsync(int mapId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var map = await uow.Maps.GetByIdAsync(mapId, tracking: false, cancellationToken);
        if (map is null)
        {
            logger.LogWarning("Map {MapId} not found in database", mapId);
            throw new InvalidOperationException($"Map {mapId} not found");
        }

        return map;
    }
}
