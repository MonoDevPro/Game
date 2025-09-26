using Arch.Core;
using Arch.LowLevel;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.ECS.Resources;
using Simulation.Core.ECS.Services;
using Simulation.Core.Options;
using Simulation.Core.Ports.ECS;
using Simulation.Core.Ports.Network;

namespace Simulation.Core.ECS.Builders;

public class ServerSimulationBuilder : ISimulationBuilder<float>
{
    private AuthorityOptions? _authorityOptions;
    private WorldOptions? _worldOptions;
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
    
    public ISimulationBuilder<float> WithRootServices(IServiceProvider services)
    {
        _rootServices = services;
        return this;
    }
    
    public GroupSystems Build()
    {
        if (_authorityOptions is null || _worldOptions is null || _rootServices is null)
            throw new InvalidOperationException("AuthorityOptions, WorldOptions e RootServices devem ser fornecidos.");

        // 1) Criar World fora do ECS pipeline
        var world = World.Create(
            chunkSizeInBytes: _worldOptions.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: _worldOptions.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: _worldOptions.ArchetypeCapacity,
            entityCapacity: _worldOptions.EntityCapacity);

        // 2) Resolver dependências externas (apenas aqui)
        var resourceContext = new ResourceContext(_rootServices, world);
        
        // 5) Construir pipeline sem IServiceProvider no hot path
        var pipeline = new GroupSystems(world, resourceContext, _authorityOptions);
        pipeline.Initialize();
        return pipeline;
    }
}