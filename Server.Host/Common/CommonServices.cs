using Game.Application;
using Game.Infrastructure.LiteNetLib;
using Game.Infrastructure.Serialization;
using Game.Infrastructure.Shared;
using Game.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Server.Host.Common;

public static class CommonServices
{
    public static IServiceCollection AddGameDatabase(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<GameDbContext>((ctx, options) =>
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
        
        return services;
    }
}