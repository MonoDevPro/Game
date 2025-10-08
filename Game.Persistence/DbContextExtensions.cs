using Game.Persistence.Interceptors;
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
        services.AddSingleton<AuditableEntityInterceptor>();
        
        services.AddDbContext<GameDbContext>((ctx, options) =>
        {
            options.UseSqlite(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(GameDbContext).Assembly.FullName);
                sqlOptions.CommandTimeout(30);
            });
            
            options.AddInterceptors(ctx.GetRequiredService<AuditableEntityInterceptor>()); 

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
