using Arch.Core;
using Simulation.Core.ECS.Indexes.Map;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Builders;

public class ServerSimulationBuilder : ISimulationBuilder<float>
{
    private WorldOptions? _worldOptions;
    private MapService? _mapService;
    private IServiceProvider? _rootServices;
    
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
        if (_worldOptions is null || _mapService is null || _rootServices is null)
            throw new InvalidOperationException("WorldOptions, MapService e RootServices devem ser fornecidos.");
        
        var world = World.Create(
            chunkSizeInBytes: _worldOptions.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: _worldOptions.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: _worldOptions.ArchetypeCapacity,
            entityCapacity: _worldOptions.EntityCapacity);
        
        var worldManager = new WorldManager(_mapService);
        var pipeline = new GroupSystems(_rootServices, world, worldManager, isServer: true);
        
        pipeline.Initialize();
        return (pipeline, world, worldManager);
    }
}