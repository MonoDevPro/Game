using Game.Application;
using Game.Infrastructure.LiteNetLib;
using Game.Infrastructure.Serialization;
using Game.Persistence;
using Server.Host.Common;

namespace Server.Host.WorldServer;

public static class WorldServerServices
{
    public static IServiceCollection AddWorldServerServices(
        this IServiceCollection services, 
        string connectionString,
        string netServerKey)
    {
        services.AddGameDatabase(connectionString);
        services.AddScoped<ICharacterRepository, EfCharacterRepository>();
        services.AddScoped<ICharacterVocationRepository, EfCharacterVocationRepository>();
        services.AddScoped<IEnterTicketService, EfEnterTicketService>();
        services.AddScoped<WorldUseCases>();
        services.AddSingleton<IMessageSerializer, MemoryPackMessageSerializer>();
        services.AddKeyedSingleton<NetServer>(netServerKey);
        services.AddHostedService<WorldServerWorker>(a => new WorldServerWorker(
            a.GetRequiredService<IServiceScopeFactory>(),
            a.GetRequiredKeyedService<NetServer>(netServerKey)));

        return services;
    }
}