using Game.Persistence.Interceptors;
using Game.Persistence.Interfaces;
using Game.Persistence.Interfaces.Repositories;
using Game.Persistence.Repositories;
using Game.Persistence.Services;
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
        
        // ✅ Registrar PlayerPersistenceService
        services.AddScoped<IPlayerPersistenceService, PlayerPersistenceService>();

        return services;
    }
}
