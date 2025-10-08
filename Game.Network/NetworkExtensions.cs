using Game.Abstractions.Network;
using Game.Network.Adapters;
using LiteNetLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Game.Network;

public static class NetworkExtensions
{
    public static IServiceCollection AddNetworking(
        this IServiceCollection services, 
        NetworkOptions options)
    {
        services.AddSingleton<INetworkManager>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            
            var packetProcessor = new PacketProcessor(loggerFactory.CreateLogger<PacketProcessor>());
            
            var listener = new NetworkListener(options.ConnectionKey, packetProcessor, loggerFactory.CreateLogger<NetworkListener>());

            var net = new NetManager(listener)
            {
                DisconnectTimeout = options.DisconnectTimeoutMs,
                ChannelsCount = (byte)Enum.GetValues(typeof(NetworkChannel)).Length,
                PingInterval = options.PingIntervalMs
            };

            var packetSender = new PacketSender(net, packetProcessor);

            return new NetworkManager(loggerFactory.CreateLogger<NetworkManager>(), options, net, listener,
                packetSender, packetProcessor);
        });
        
        return services;
    }
}