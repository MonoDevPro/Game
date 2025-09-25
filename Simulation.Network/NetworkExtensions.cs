using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;

namespace Simulation.Network;

public static class NetworkServiceCollectionExtensions
{
    public static IServiceCollection AddNetworking(this IServiceCollection services)
    {
        services.AddSingleton<NetworkManager>(sp => new NetworkManager(
            sp.GetRequiredService<IOptions<NetworkOptions>>().Value,
            sp.GetRequiredService<IOptions<AuthorityOptions>>().Value,
            sp.GetRequiredService<ILoggerFactory>()));
        services.AddSingleton<INetworkManager>(sp => sp.GetRequiredService<NetworkManager>());
        services.AddSingleton<IChannelProcessorFactory>(sp => sp.GetRequiredService<NetworkManager>().ProcessorFactory);
        return services;
    }
}