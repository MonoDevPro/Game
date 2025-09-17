using Arch.Core;
using Simulation.Core.ECS.Indexes.Map;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Builders;

public class ClientSimulationBuilder : ISimulationBuilder<float>
{
    private MapService? _mapService;
    private WorldOptions? _worldOptions;
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

        // registra pipeline e provider no container
        var pipeline = new GroupSystems(_rootServices, world, worldManager, isServer: false);
        
        pipeline.Initialize();
        return (pipeline, world, worldManager);
    }
    
}