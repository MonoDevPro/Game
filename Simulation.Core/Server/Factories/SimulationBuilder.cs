using Arch.Core;
using Arch.System;
using LiteNetLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.Server.Persistence.Contracts;
using Simulation.Core.Server.Snapshot;
using Simulation.Core.Server.Staging;
using Simulation.Core.Server.Systems;
using Simulation.Core.Shared.Network;
using Simulation.Core.Shared.Options;
using Simulation.Core.Shared.Templates;
using Simulation.Generated.Network;

namespace Simulation.Core.Server.Factories;

/// <summary>
/// Define um contrato para a construção de uma pipeline de simulação completa.
/// </summary>
public interface ISimulationBuilder
{
    /// <summary>
    /// Fornece as opções de configuração do mundo ECS.
    /// </summary>
    ISimulationBuilder WithWorldOptions(WorldOptions options);

    /// <summary>
    /// Fornece as opções de configuração do sistema espacial.
    /// </summary>
    ISimulationBuilder WithSpatialOptions(SpatialOptions options);
    
    ISimulationBuilder WithNetworkOptions(NetworkOptions options);

    /// <summary>
    /// Fornece o contentor de serviços da aplicação principal para resolver dependências externas.
    /// </summary>
    ISimulationBuilder WithRootServices(IServiceProvider services);


    /// <summary>
    /// Constrói e retorna o grupo de sistemas (a pipeline) configurado.
    /// </summary>
    /// Um Group pronto a ser executado.
    Group<float> Build();
}

public class SimulationBuilder : ISimulationBuilder
{
    private WorldOptions? _worldOptions;
    private SpatialOptions? _spatialOptions;
    private NetworkOptions? _networkOptions;
    private IServiceProvider? _rootServices;

    public ISimulationBuilder WithWorldOptions(WorldOptions options)
    {
        _worldOptions = options;
        return this;
    }

    public ISimulationBuilder WithSpatialOptions(SpatialOptions options)
    {
        _spatialOptions = options;
        return this;
    }

    public ISimulationBuilder WithRootServices(IServiceProvider services)
    {
        _rootServices = services;
        return this;
    }
    
    public ISimulationBuilder WithNetworkOptions(NetworkOptions options)
    {
        _networkOptions = options;
        return this;
    }

    public Group<float> Build()
    {
        // Validação para garantir que todas as dependências foram fornecidas.
        if (_worldOptions is null || _spatialOptions is null || _networkOptions is null || _rootServices is null)
        {
            throw new InvalidOperationException("WorldOptions, SpatialOptions, NetworkOptions and RootServices must be provided before building.");
        }

        // 1. Cria o World com as opções fornecidas.
        var world = World.Create(
            chunkSizeInBytes: _worldOptions.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: _worldOptions.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: _worldOptions.ArchetypeCapacity,
            entityCapacity: _worldOptions.EntityCapacity
        );
        
        var systemServices = new ServiceCollection();
        
        systemServices.AddTransient(typeof(ILogger<>), typeof(Logger<>));

        // 2. Regista a instância do World e os serviços externos do contentor principal.
        systemServices.AddSingleton(world);
        systemServices.AddSingleton(_worldOptions);
        systemServices.AddSingleton(_spatialOptions);
        systemServices.AddSingleton(_networkOptions);
        systemServices.AddSingleton(_rootServices.GetRequiredService<DebugOptions>());
        systemServices.AddSingleton(_rootServices.GetRequiredService<IPlayerStagingArea>());
        systemServices.AddSingleton(_rootServices.GetRequiredService<IMapStagingArea>());
        systemServices.AddSingleton(_rootServices.GetRequiredService<ILoggerFactory>());
        systemServices.AddSingleton(_rootServices.GetRequiredService<EventBasedNetListener>());
        systemServices.AddSingleton(_rootServices.GetRequiredService<IRepository<int, MapData>>());
        systemServices.AddSingleton(_rootServices.GetRequiredService<IRepository<int, PlayerData>>());
        systemServices.AddSingleton<IPlayerSnapshotArea, PlayerSnapshotArea>();
        systemServices.AddSingleton<NetworkManager>();
        
        systemServices.AddSingleton<NetworkSystem>();
        systemServices.AddSingleton<StagingProcessorSystem>();
        systemServices.AddSingleton<EntityIndexSystem>();
        systemServices.AddSingleton<SpatialIndexSystem>();
        systemServices.AddSingleton<EntityFactorySystem>();
        
        systemServices.AddSingleton<MovementSystem>();
        
        systemServices.AddSingleton<EntitySaveSystem>();
        systemServices.AddSingleton<EntitySnapshotSystem>();
        systemServices.AddSingleton<EntityDestructorSystem>();
        systemServices.AddSingleton<GeneratedServerSyncSystem>();
        
        // 4. Constrói o provedor de serviços exclusivo para o ECS
        var ecsServiceProvider = systemServices.BuildServiceProvider();
        
        // 5. Cria o grupo de sistemas (a pipeline)
        var pipeline = new Group<float>("SimulationServer Group");
        // 6. Adiciona os sistemas ao grupo na ordem de execução correta
        AddSystem<NetworkSystem>(ecsServiceProvider, pipeline);
        AddSystem<StagingProcessorSystem>(ecsServiceProvider, pipeline);
        AddSystem<EntityIndexSystem>(ecsServiceProvider, pipeline);
        AddSystem<SpatialIndexSystem>(ecsServiceProvider, pipeline);
        AddSystem<EntityFactorySystem>(ecsServiceProvider, pipeline);
        
        AddSystem<MovementSystem>(ecsServiceProvider, pipeline);
        
        AddSystem<EntitySaveSystem>(ecsServiceProvider, pipeline);
        AddSystem<EntitySnapshotSystem>(ecsServiceProvider, pipeline);
        AddSystem<EntityDestructorSystem>(ecsServiceProvider, pipeline);
        AddSystem<GeneratedServerSyncSystem>(ecsServiceProvider, pipeline);
        
        // 7. Inicializa todos os sistemas e retorna a pipeline pronta.
        pipeline.Initialize();
        return pipeline;
    }
    
    private void AddSystem<T>(IServiceProvider provider, Group<float> group) where T : ISystem<float>
    {
        group.Add(provider.GetRequiredService<T>());
    }
}