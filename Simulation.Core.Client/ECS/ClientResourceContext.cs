using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Builders;
using Simulation.Core.ECS.Resource;
using Simulation.Core.ECS.Utils;
using Simulation.Core.Ports.Network;

namespace Simulation.Core.Client.ECS;

/// <summary>
/// Contexto de containers Resources<T> do Arch.LowLevel para tipos gerenciados
/// referenciados por componentes via Handle<T>.
/// </summary>
public sealed class ClientResourceContext : ResourceContext
{
    public readonly PlayerIndexResource PlayerIndex;
    public readonly SpatialIndexResource SpatialIndex;
    public readonly PlayerNetResource PlayerNet;
    public readonly PlayerFactoryResource PlayerFactory;
    public readonly ILoggerFactory LoggerFactory;

    public ClientResourceContext(IServiceProvider provider, World world) : base(provider, world)
    {
        PlayerIndex = new PlayerIndexResource(world);
        SpatialIndex = new SpatialIndexResource(provider.GetRequiredService<MapService>());
        PlayerNet = new PlayerNetResource(world, PlayerIndex, provider.GetRequiredService<INetworkManager>());
        PlayerFactory = new PlayerFactoryResource(world, PlayerIndex, SpatialIndex);
        LoggerFactory = provider.GetRequiredService<ILoggerFactory>();
    }
}
