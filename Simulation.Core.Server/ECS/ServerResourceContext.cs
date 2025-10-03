using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Builders;
using Simulation.Core.ECS.Resource;
using Simulation.Core.ECS.Utils;
using Simulation.Core.Ports.ECS;
using Simulation.Core.Ports.Network;

namespace Simulation.Core.Server.ECS;

/// <summary>
/// Contexto de containers Resources<T> do Arch.LowLevel para tipos gerenciados
/// referenciados por componentes via Handle<T>.
/// </summary>
public sealed class ServerResourceContext : ResourceContext
{
    public readonly PlayerSaveResource PlayerSave;
    public readonly PlayerIndexResource PlayerIndex;
    public readonly SpatialIndexResource SpatialIndex;
    public readonly PlayerFactoryResource PlayerFactory;
    public readonly PlayerNetResource PlayerNet;
    
    private readonly ILoggerFactory _loggerFactory;
    public ILogger<T> GetLogger<T>() => _loggerFactory.CreateLogger<T>();

    public ServerResourceContext(IServiceProvider provider, World world, MapService service) : base(provider, world)
    {
        PlayerSave = new PlayerSaveResource(world, provider.GetRequiredService<IWorldSaver>());
        PlayerIndex = new PlayerIndexResource(world);
        SpatialIndex = new SpatialIndexResource(service);
        PlayerFactory = new PlayerFactoryResource(world, PlayerIndex, SpatialIndex);
        PlayerNet = new PlayerNetResource(world, PlayerIndex, provider.GetRequiredService<INetworkManager>());
        _loggerFactory = provider.GetRequiredService<ILoggerFactory>();
    }
}
