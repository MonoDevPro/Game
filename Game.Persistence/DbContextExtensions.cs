using Game.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Game.Persistence;

/// <summary>
/// Extensões para configuração do DbContext
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:16:27
/// </summary>
public static class DbContextExtensions
{
    public static IServiceCollection AddGameDatabase(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<GameDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(GameDbContext).Assembly.FullName);
                sqlOptions.CommandTimeout(30);
            });

            // Configurações de performance
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            
            // Apenas em desenvolvimento
#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
#endif
        });

        // Registrar UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        // Aplicar migrações pendentes
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        var migrations = pendingMigrations as string[] ?? pendingMigrations.ToArray();
        if (migrations.Length != 0)
        {
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Applying {migrations.Count()} migrations... - MonoDevPro");
            await context.Database.MigrateAsync();
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Migrations applied successfully - MonoDevPro");
        }
        else
        {
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Database is up to date - MonoDevPro");
        }
    }
}
