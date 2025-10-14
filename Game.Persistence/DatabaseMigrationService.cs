using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
            await MigrateDatabaseAsync(serviceProvider);
            logger.LogInformation("Database ready");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply migrations");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    
    private async Task MigrateDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        // Verificar se existem migrações
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

        var pending = pendingMigrations.ToArray();
        var applied = appliedMigrations.ToArray();

        // Se não há migrações aplicadas nem pendentes, criar o banco direto (desenvolvimento)
        if (pending.Length == 0 && applied.Length == 0)
        {
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] No migrations found. Creating database schema... - MonoDevPro");
            await context.Database.EnsureCreatedAsync();
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Database schema created successfully - MonoDevPro");
        }
        else if (pending.Length != 0)
        {
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Applying {pending.Length} migrations... - MonoDevPro");
            await context.Database.MigrateAsync();
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Migrations applied successfully - MonoDevPro");
        }
        else
        {
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Database is up to date - MonoDevPro");
        }
    }
}
