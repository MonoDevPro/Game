using LiteNetLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.Network;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;
using Simulation.Network.Channel;
using Simulation.Network.Packet;

namespace Simulation.Network;

public static class NetworkServiceCollectionExtensions
{
    public static IServiceCollection AddNetworking(this IServiceCollection services)
    {
        services.AddSingleton<NetworkOptions>(sp => sp.GetRequiredService<IOptions<NetworkOptions>>().Value);
        services.AddSingleton<NetworkManager>();
        services.AddSingleton<INetworkManager>(sp => sp.GetRequiredService<NetworkManager>());
        services.AddSingleton<IChannelProcessorFactory>(sp => sp.GetRequiredService<NetworkManager>().ProcessorFactory);
        return services;
    }
}