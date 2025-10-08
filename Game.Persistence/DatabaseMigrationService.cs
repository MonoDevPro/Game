using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Game.Persistence;

/// <summary>
/// Runs database migrations before the rest of the server services start ticking.
/// </summary>
public sealed class DatabaseMigrationService(IServiceProvider serviceProvider, ILogger<DatabaseMigrationService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Applying database migrations (if any)...");
            await serviceProvider.MigrateDatabaseAsync();
            logger.LogInformation("Database ready");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply migrations");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
