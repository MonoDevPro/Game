using Game.ECS.Services;
using Game.ECS.Services.Snapshot.Sync;
using Game.Network.Abstractions;
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
        services.AddSingleton<NetworkManager>(a => BuildNetworkManager(a, options));
        services.AddSingleton<INetworkManager>(sp => sp.GetRequiredService<NetworkManager>());
        services.AddSingleton<INetSync>(sp => sp.GetRequiredService<NetworkManager>());
        return services;
    }
        
        
        private static NetworkManager BuildNetworkManager(IServiceProvider sp, NetworkOptions options)
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var packetProcessor = new PacketProcessor(loggerFactory.CreateLogger<PacketProcessor>());
            var listener = new NetworkListener(options.ConnectionKey, packetProcessor, loggerFactory.CreateLogger<NetworkListener>());
            var net = new NetManager(listener)
            {
                DisconnectTimeout = options.DisconnectTimeoutMs,
                ChannelsCount = (byte)Enum.GetValues(typeof(NetworkChannel)).Length,
                PingInterval = options.PingIntervalMs,
                UnconnectedMessagesEnabled = true,
            };
            var packetSender = new PacketSender(net, packetProcessor);
            return new NetworkManager(loggerFactory.CreateLogger<NetworkManager>(), options, net, listener,
                packetSender, packetProcessor);
        }
    }