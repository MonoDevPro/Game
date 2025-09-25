using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.Options;
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
            throw new InvalidOperationException("AuthorityOptions, WorldOptions, MapService e RootServices devem ser fornecidos.");
        
        var endpoint = 
            _rootServices.GetRequiredService<IChannelProcessorFactory>()
            .CreateOrGet(NetworkChannel.Simulation);
        
        var pipeline = new GroupSystems(_rootServices, endpoint, _authorityOptions, _worldOptions);
        
        pipeline.Initialize();
        return pipeline;
    }
}