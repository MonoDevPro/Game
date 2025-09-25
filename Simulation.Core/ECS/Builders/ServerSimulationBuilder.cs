using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.ECS.Services;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;
using Simulation.Core.Ports;

namespace Simulation.Core.ECS.Builders;

public class ServerSimulationBuilder : ISimulationBuilder<float>
{
    private AuthorityOptions? _authorityOptions;
    private WorldOptions? _worldOptions;
    private MapService? _mapService;
    private IServiceProvider? _rootServices;
    
    public ISimulationBuilder<float> WithAuthorityOptions(AuthorityOptions options)
    {
        _authorityOptions = options;
        return this;
    }
    
    public ISimulationBuilder<float> WithWorldOptions(WorldOptions options)
    {
        _worldOptions = options;
        return this;
    }
    
    public ISimulationBuilder<float> WithMapService(MapService service)
    {
        _mapService = service;
        return this;
    }

    public ISimulationBuilder<float> WithRootServices(IServiceProvider services)
    {
        _rootServices = services;
        return this;
    }
    
    public (GroupSystems Systems, World World, WorldManager WorldManager) Build()
    {
        if (_authorityOptions is null || _worldOptions is null || _mapService is null || _rootServices is null)
            throw new InvalidOperationException("AuthorityOptions, WorldOptions, MapService e RootServices devem ser fornecidos.");
        
        var mapService = _mapService;
        var worldSaver = _rootServices.GetRequiredService<IWorldSaver>();
        var networkManager = _rootServices.GetRequiredService<INetworkManager>();
        var channelFactory = _rootServices.GetRequiredService<IChannelProcessorFactory>();
        
        var worldManager = new WorldManager(
            mapService, 
            worldSaver, 
            networkManager, 
            channelFactory.CreateOrGet(NetworkChannel.Simulation));
        
        var factoryLogger = _rootServices.GetRequiredService<ILoggerFactory>();
        
        var world = World.Create(
            chunkSizeInBytes: _worldOptions.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: _worldOptions.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: _worldOptions.ArchetypeCapacity,
            entityCapacity: _worldOptions.EntityCapacity);
        
        var pipeline = new GroupSystems(factoryLogger, world, worldManager, _authorityOptions);
        
        pipeline.Initialize();
        return (pipeline, world, worldManager);
    }
}