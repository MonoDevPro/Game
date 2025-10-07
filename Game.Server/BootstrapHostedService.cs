using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Game.Server;

/// <summary>
/// Runs database migrations before the rest of the server services start ticking.
/// </summary>
public sealed class BootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BootstrapHostedService> _logger;

    public BootstrapHostedService(IServiceProvider serviceProvider, ILogger<BootstrapHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Applying database migrations (if any)...");
            await _serviceProvider.MigrateDatabaseAsync();
            _logger.LogInformation("Database ready");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply migrations");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
