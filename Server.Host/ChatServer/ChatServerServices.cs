using Game.Infrastructure.LiteNetLib;
using Game.Infrastructure.Serialization;

namespace Server.Host.ChatServer;

public static class ChatServerServices
{
    public static IServiceCollection AddChatServerServices(
        this IServiceCollection services, 
        string connectionString, 
        string netServerKey)
    {
        services.AddSingleton<IMessageSerializer, MemoryPackMessageSerializer>();
        services.AddKeyedSingleton<NetServer>(netServerKey);
        services.AddHostedService<ChatServerWorker>(a => new ChatServerWorker(
            a.GetRequiredKeyedService<NetServer>(netServerKey)));
        
        return services;
    }
}