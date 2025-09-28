using Application.Models.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.Options;
using Simulation.Core.Ports.Network;

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
        return services;
    }
}