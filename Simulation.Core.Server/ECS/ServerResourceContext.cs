using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.ECS.Builders;
using Simulation.Core.ECS.Resource;
using Simulation.Core.ECS.Services;
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

    public ServerResourceContext(IServiceProvider provider, World world) : base(provider, world)
    {
        PlayerSave = new PlayerSaveResource(world, provider.GetRequiredService<IWorldSaver>());
        PlayerIndex = new PlayerIndexResource(world);
        SpatialIndex = new SpatialIndexResource(provider.GetRequiredService<MapService>());
        PlayerFactory = new PlayerFactoryResource(world, PlayerIndex, SpatialIndex, PlayerSave);
        PlayerNet = new PlayerNetResource(world, PlayerIndex, 
            provider.GetRequiredService<INetworkManager>());
    }
}
