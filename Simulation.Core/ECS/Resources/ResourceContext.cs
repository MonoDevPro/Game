using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.ECS.Services;
using Simulation.Core.Ports.ECS;
using Simulation.Core.Ports.Network;

namespace Simulation.Core.ECS.Resources;

/// <summary>
/// Pure ECS resource context that provides lazy-initialized resources to systems.
/// Uses factory pattern to reduce constructor complexity and enable proper dependency ordering.
/// </summary>
public sealed class ResourceContext
{
    private readonly ResourceFactory _factory;
    
    // Lazy initialization to handle dependency ordering properly
    private readonly Lazy<PlayerIndexResource> _playerIndex;
    private readonly Lazy<SpatialIndexResource> _spatialIndex;
    private readonly Lazy<PlayerFactoryResource> _playerFactory;
    private readonly Lazy<PlayerSaveResource> _playerSave;
    private readonly Lazy<IChannelEndpoint> _networkEndpoint;

    public ResourceContext(IServiceProvider provider, World world)
    {
        _factory = new ResourceFactory(provider, world);
        
        // Initialize resources with proper dependency ordering using lazy evaluation
        _playerIndex = new Lazy<PlayerIndexResource>(_factory.CreatePlayerIndex);
        _spatialIndex = new Lazy<SpatialIndexResource>(_factory.CreateSpatialIndex);
        _networkEndpoint = new Lazy<IChannelEndpoint>(_factory.CreateNetworkEndpoint);
        _playerSave = new Lazy<PlayerSaveResource>(_factory.CreatePlayerSave);
        
        // PlayerFactory depends on PlayerIndex and SpatialIndex, so it's initialized after them
        _playerFactory = new Lazy<PlayerFactoryResource>(() => 
            _factory.CreatePlayerFactory(PlayerIndex, SpatialIndex));
    }

    /// <summary>
    /// Gets the player index resource for tracking player entities.
    /// </summary>
    public PlayerIndexResource PlayerIndex => _playerIndex.Value;

    /// <summary>
    /// Gets the spatial index resource for world queries.
    /// </summary>
    public SpatialIndexResource SpatialIndex => _spatialIndex.Value;

    /// <summary>
    /// Gets the player factory resource for player entity management.
    /// </summary>
    public PlayerFactoryResource PlayerFactory => _playerFactory.Value;

    /// <summary>
    /// Gets the player save resource for persisting player data.
    /// </summary>
    public PlayerSaveResource PlayerSave => _playerSave.Value;

    /// <summary>
    /// Gets the network endpoint for simulation communication.
    /// </summary>
    public IChannelEndpoint NetworkEndpoint => _networkEndpoint.Value;
}
