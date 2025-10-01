using Arch.Core;
using Arch.System;
using GameWeb.Application.Common.Options;

namespace Simulation.Core.ECS.Builders;

/// <summary>
/// Base class for simulation builders that eliminates code duplication
/// and provides common functionality for building ECS pipelines.
/// </summary>
public abstract class BaseSimulationBuilder<TContext> : ISimulationBuilder<float>
    where TContext : ResourceContext
{
    protected AuthorityOptions? AuthorityOptions;
    protected WorldOptions? WorldOptions;
    protected IServiceProvider? RootServices;

    public ISimulationBuilder<float> WithAuthorityOptions(AuthorityOptions options)
    {
        AuthorityOptions = options;
        return this;
    }

    public ISimulationBuilder<float> WithWorldOptions(WorldOptions options)
    {
        WorldOptions = options;
        return this;
    }

    public ISimulationBuilder<float> WithRootServices(IServiceProvider services)
    {
        RootServices = services;
        return this;
    }

    public SystemGroup Build()
    {
        ValidateRequiredDependencies();

        // Create World outside ECS pipeline
        var world = CreateWorld();

        // Resolve external dependencies (only here)
        var resourceContext = CreateResourceContext(world);

        // Build pipeline without IServiceProvider in hot path
        var pipeline = new SystemGroup("Simulation Systems", world);

        pipeline.Add(RegisterComponentUpdate(world, resourceContext));
        pipeline.Add(CreateSystems(world, resourceContext));
        pipeline.Add(RegisterComponentPost(world, resourceContext));
        
        pipeline.Initialize();
        return pipeline;
    }

    /// <summary>
    /// Validates that all required dependencies are provided before building.
    /// </summary>
    private void ValidateRequiredDependencies()
    {
        if (AuthorityOptions is null || WorldOptions is null || RootServices is null)
            throw new InvalidOperationException("AuthorityOptions, WorldOptions e RootServices devem ser fornecidos.");
    }

    /// <summary>
    /// Creates the Arch World with configured options.
    /// </summary>
    private World CreateWorld()
    {
        return World.Create(
            chunkSizeInBytes: WorldOptions!.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: WorldOptions.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: WorldOptions.ArchetypeCapacity,
            entityCapacity: WorldOptions.EntityCapacity);
    }

    /// <summary>
    /// Creates the resource context with all necessary dependencies.
    /// Can be overridden by derived classes for specialized resource creation.
    /// </summary>
    protected abstract TContext CreateResourceContext(World world);
    protected abstract ISystem<float> RegisterComponentUpdate(World world, TContext resources);
    protected abstract ISystem<float> RegisterComponentPost(World world, TContext resources);
    protected abstract ISystem<float> CreateSystems(World world, TContext resources);

}