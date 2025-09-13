using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Server.Staging;
using Simulation.Core.ECS.Server.Systems;
using Simulation.Core.ECS.Shared.Indexes;
using Simulation.Core.Network;
using Simulation.Core.Options;
using Simulation.Generated.Network;

namespace Simulation.Core.ECS.Server;

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

    public ISimulationBuilder WithNetworkOptions(NetworkOptions options)
    {
        _networkOptions = options;
        return this;
    }

    public ISimulationBuilder WithRootServices(IServiceProvider services)
    {
        _rootServices = services;
        return this;
    }

    public Group<float> Build()
    {
        // Validação para garantir que todas as dependências foram fornecidas.
        if (_worldOptions is null || _spatialOptions is null || _networkOptions is null || _rootServices is null)
            throw new InvalidOperationException("WorldOptions, SpatialOptions and RootServices must be provided before building.");

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
        systemServices.AddSingleton(_spatialOptions);
        systemServices.AddSingleton(_rootServices.GetRequiredService<IPlayerStagingArea>());
        systemServices.AddSingleton(_rootServices.GetRequiredService<IMapStagingArea>());
        systemServices.AddSingleton(_rootServices.GetRequiredService<ILoggerFactory>());
        systemServices.AddSingleton<NetworkManager>( sp => 
            new NetworkManager(world, 
                sp.GetRequiredService<IPlayerIndex>(), 
                _networkOptions)
        );
        
        systemServices.AddSingleton<NetworkSystem>();
        systemServices.AddSingleton<StagingProcessorSystem>();
        systemServices.AddSingleton<EntityIndexSystem>();
        systemServices.AddSingleton<SpatialIndexSystem>();
        systemServices.AddSingleton<EntityFactorySystem>();
        
        systemServices.AddSingleton<MovementSystem>();
        
        systemServices.AddSingleton<EntitySaveSystem>();
        systemServices.AddSingleton<EntityDestructorSystem>();
        systemServices.AddSingleton<EntitySnapshotSystem>();
        systemServices.AddSingleton<GeneratedServerSyncSystem>();
        
        systemServices.AddSingleton<IPlayerIndex>(sp => sp.GetRequiredService<EntityIndexSystem>());
        systemServices.AddSingleton<IMapIndex>(sp => sp.GetRequiredService<EntityIndexSystem>());
        
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