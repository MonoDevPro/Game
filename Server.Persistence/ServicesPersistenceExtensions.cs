using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Server.Persistence.Context;
using Server.Persistence.Hosted;
using Server.Persistence.Repositories;
using Server.Persistence.Staging;
using Simulation.Core.ECS.Shared.Staging;
using Simulation.Core.Persistence.Contracts;
using Simulation.Core.Persistence.Models;

namespace Server.Persistence;

public static class ServicesPersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        // 1. Adiciona o DbContext ao contêiner de serviços
        // O tempo de vida padrão aqui é 'Scoped' (uma instância por requisição HTTP).
        services.AddDbContext<SimulationDbContext>(options =>
                options.UseSqlite(connectionString))
            .AddOptions<DbContextOptions<SimulationDbContext>>();

        // Registra a fila de tarefas como Singleton
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        // Registra o serviço que vai consumir a fila (o QueuedHostedService que vimos antes)
        services.AddHostedService<QueuedHostedService>();
        
        services.AddSingleton<IMapStagingArea, MapStagingArea>();
        services.AddSingleton<IPlayerStagingArea, PlayerStagingArea>();

        services.AddScoped<IMapRepository, MapRepository>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        
        return services;
    }
}