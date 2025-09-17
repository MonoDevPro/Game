using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Staging;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Builders;

public class ServerSimulationBuilder : ISimulationBuilder<float>
{
    private WorldOptions? _worldOptions;
    private IServiceProvider? _rootServices;
    
    private readonly List<(Type type, SyncOptions options)> _syncRegistrations = [];

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
    
    public (PipelineSystems Systems, World World) Build()
    {
        if (_worldOptions is null || _rootServices is null)
            throw new InvalidOperationException("WorldOptions e RootServices devem ser fornecidos.");
        
        // registra pipeline e provider no container
        var pipeline = new PipelineSystems(_rootServices, _worldOptions, isServer: true);
        
        pipeline.Initialize();
        return (pipeline, pipeline.World);
    }
}