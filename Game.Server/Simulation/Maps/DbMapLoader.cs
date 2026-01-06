using Game.Domain.Entities;
using Game.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace Game.Server.Simulation.Maps;

/// <summary>
/// Loads maps from the database at server startup.
/// </summary>
internal sealed class DbMapLoader(IServiceScopeFactory scopeFactory, ILogger<DbMapLoader> logger) : IMapLoader
{
    public async Task<IReadOnlyList<Map>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var maps = await uow.Maps.GetAllAsync(cancellationToken);
        if (maps.Length == 0)
        {
            logger.LogWarning("No maps found in database. Simulation will start without registered maps.");
            return Array.Empty<Map>();
        }

        return maps;
    }
}