using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.ECS.Services;
using Simulation.Core.Ports.ECS;
using Simulation.Core.Ports.Network;

namespace Simulation.Core.ECS.Resources;

/// <summary>
/// Contexto de containers Resources<T> do Arch.LowLevel para tipos gerenciados
/// referenciados por componentes via Handle<T>.
/// </summary>
public sealed class ResourceContext
{
    public readonly PlayerIndexResource PlayerIndex;
    public readonly SpatialIndexResource SpatialIndex;
    public readonly PlayerFactoryResource PlayerFactory;
    public readonly PlayerSaveResource PlayerSave;
    public readonly IChannelEndpoint NetworkEndpoint;

    public ResourceContext(IServiceProvider provider, World world)
    {
        PlayerIndex = new PlayerIndexResource(world);
        SpatialIndex = new SpatialIndexResource(provider.GetRequiredService<MapService>());
        PlayerFactory = new PlayerFactoryResource(world, PlayerIndex, SpatialIndex);
        PlayerSave = new PlayerSaveResource(world, provider.GetRequiredService<IWorldSaver>());
        NetworkEndpoint = provider.GetRequiredService<IChannelProcessorFactory>().CreateOrGet(NetworkChannel.Simulation);
    }
}
