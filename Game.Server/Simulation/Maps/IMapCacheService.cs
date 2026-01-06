using Game.Domain.Entities;

namespace Game.Server.Simulation.Maps;

public interface IMapCacheService
{
    Task<Map> GetMapAsync(int mapId, CancellationToken cancellationToken = default);
    Task InvalidateAsync(int mapId);
    Task InvalidateAllAsync();
}
