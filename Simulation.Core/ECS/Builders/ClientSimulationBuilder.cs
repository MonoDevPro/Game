using System.Linq.Expressions;
using System.Reflection;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Builders;

public class ClientSimulationBuilder : ISimulationBuilder<float>
{
    private WorldOptions? _worldOptions;
    private IServiceProvider? _rootServices;

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
            throw new InvalidOperationException("WorldOptions, RootServices e NetworkOptions devem ser fornecidos.");

        // registra pipeline e provider no container
        var pipeline = new PipelineSystems(_rootServices, _worldOptions, isServer: false);
        
        pipeline.Initialize();
        return (pipeline, pipeline.World);
    }
    
}