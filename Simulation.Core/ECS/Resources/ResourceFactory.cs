using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.ECS.Services;
using Simulation.Core.Ports.ECS;
using Simulation.Core.Ports.Network;

namespace Simulation.Core.ECS.Resources;

/// <summary>
/// Factory responsible for creating and configuring ECS resources.
/// Provides lazy initialization and separates resource creation logic from ResourceContext.
/// </summary>
public class ResourceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly World _world;

    public ResourceFactory(IServiceProvider serviceProvider, World world)
    {
        _serviceProvider = serviceProvider;
        _world = world;
    }

    /// <summary>
    /// Creates the player index resource for tracking player entities.
    /// </summary>
    public PlayerIndexResource CreatePlayerIndex()
    {
        return new PlayerIndexResource(_world);
    }

    /// <summary>
    /// Creates the spatial index resource for world queries.
    /// </summary>
    public SpatialIndexResource CreateSpatialIndex()
    {
        var mapService = _serviceProvider.GetRequiredService<MapService>();
        return new SpatialIndexResource(mapService);
    }

    /// <summary>
    /// Creates the player factory resource for player entity management.
    /// Requires playerIndex and spatialIndex to be created first.
    /// </summary>
    public PlayerFactoryResource CreatePlayerFactory(PlayerIndexResource playerIndex, SpatialIndexResource spatialIndex)
    {
        return new PlayerFactoryResource(_world, playerIndex, spatialIndex);
    }

    /// <summary>
    /// Creates the player save resource for persisting player data.
    /// </summary>
    public PlayerSaveResource CreatePlayerSave()
    {
        var worldSaver = _serviceProvider.GetRequiredService<IWorldSaver>();
        return new PlayerSaveResource(_world, worldSaver);
    }

    /// <summary>
    /// Creates the network endpoint for simulation communication.
    /// </summary>
    public IChannelEndpoint CreateNetworkEndpoint()
    {
        var factory = _serviceProvider.GetRequiredService<IChannelProcessorFactory>();
        return factory.CreateOrGet(NetworkChannel.Simulation);
    }
}