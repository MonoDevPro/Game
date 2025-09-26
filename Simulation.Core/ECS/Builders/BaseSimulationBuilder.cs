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

/// <summary>
/// Base class for simulation builders that eliminates code duplication
/// and provides common functionality for building ECS pipelines.
/// </summary>
public abstract class BaseSimulationBuilder : ISimulationBuilder<float>
{
    protected AuthorityOptions? _authorityOptions;
    protected WorldOptions? _worldOptions;
    protected IServiceProvider? _rootServices;

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
        ValidateRequiredDependencies();

        // Create World outside ECS pipeline
        var world = CreateWorld();

        // Resolve external dependencies (only here)
        var resourceContext = CreateResourceContext(world);

        // Build pipeline without IServiceProvider in hot path
        var pipeline = new GroupSystems(world, resourceContext, _authorityOptions!);
        pipeline.Initialize();
        return pipeline;
    }

    /// <summary>
    /// Validates that all required dependencies are provided before building.
    /// </summary>
    private void ValidateRequiredDependencies()
    {
        if (_authorityOptions is null || _worldOptions is null || _rootServices is null)
            throw new InvalidOperationException("AuthorityOptions, WorldOptions e RootServices devem ser fornecidos.");
    }

    /// <summary>
    /// Creates the Arch World with configured options.
    /// </summary>
    private World CreateWorld()
    {
        return World.Create(
            chunkSizeInBytes: _worldOptions!.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: _worldOptions.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: _worldOptions.ArchetypeCapacity,
            entityCapacity: _worldOptions.EntityCapacity);
    }

    /// <summary>
    /// Creates the resource context with all necessary dependencies.
    /// Can be overridden by derived classes for specialized resource creation.
    /// </summary>
    protected virtual ResourceContext CreateResourceContext(World world)
    {
        return new ResourceContext(_rootServices!, world);
    }
}