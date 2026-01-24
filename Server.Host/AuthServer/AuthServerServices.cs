using Game.Application;
using Game.Infrastructure.LiteNetLib;
using Game.Infrastructure.Serialization;
using Game.Infrastructure.Shared;
using Game.Persistence;
using Server.Host.Common;

namespace Server.Host.AuthServer;

public static class AuthServerServices
{
    public static IServiceCollection AddAuthServerServices(this IServiceCollection services, string connectionString, string netServerKey)
    {
        services.AddGameDatabase(connectionString);
        services.AddScoped<IAccountRepository, EfAccountRepository>();
        services.AddScoped<ICharacterRepository, EfCharacterRepository>();
        services.AddScoped<IEnterTicketService, EfEnterTicketService>();
        services.AddSingleton<ISessionService, InMemorySessionService>();
        services.AddScoped<AuthUseCases>();
        services.AddSingleton<IMessageSerializer, MemoryPackMessageSerializer>();
        services.AddKeyedSingleton<NetServer>(netServerKey);
        services.AddHostedService<AuthServerWorker>(a => new AuthServerWorker(
            a.GetRequiredService<IServiceScopeFactory>(),
            a.GetRequiredKeyedService<NetServer>(netServerKey)));
        
        return services;
    }
}