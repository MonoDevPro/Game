using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Server.Systems;
using Simulation.Core.ECS.Shared.Staging;
using Simulation.Core.ECS.Shared.Systems;
using Simulation.Core.ECS.Shared.Systems.Indexes;
using Simulation.Core.Network;
using Simulation.Core.Options;
using EntityDestructorSystem = Simulation.Core.ECS.Shared.Systems.EntityDestructorSystem;
using MovementSystem = Simulation.Core.ECS.Shared.Systems.MovementSystem;

namespace Simulation.Core.ECS.Server;

public class ServerSimulationBuilder : ISimulationBuilder<float>
{
    private WorldOptions? _worldOptions;
    private SpatialOptions? _spatialOptions;
    private NetworkOptions? _networkOptions;
    private IServiceProvider? _rootServices;
    
    private readonly List<(Type type, SyncOptions options)> _syncRegistrations = [];

    public ISimulationBuilder<float> WithWorldOptions(WorldOptions options)
    {
        _worldOptions = options;
        return this;
    }

    public ISimulationBuilder<float> WithSpatialOptions(SpatialOptions options)
    {
        _spatialOptions = options;
        return this;
    }

    public ISimulationBuilder<float> WithNetworkOptions(NetworkOptions options)
    {
        _networkOptions = options;
        return this;
    }

    public ISimulationBuilder<float> WithRootServices(IServiceProvider services)
    {
        _rootServices = services;
        return this;
    }
    
    // MELHORIA: Adicionada a restrição IEquatable para segurança de tipo em tempo de compilação.
    public ISimulationBuilder<float> WithSynchronizedComponent<T>(SyncOptions options) where T : struct, IEquatable<T>
    {
        _syncRegistrations.Add((typeof(T), options));
        return this;
    }

    public (Group<float> Group, World World) Build()
    {
        if (_worldOptions is null || _spatialOptions is null || _networkOptions is null || _rootServices is null)
            throw new InvalidOperationException("WorldOptions, SpatialOptions, RootServices e NetworkOptions devem ser fornecidos.");

        var world = World.Create(
            chunkSizeInBytes: _worldOptions.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: _worldOptions.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: _worldOptions.ArchetypeCapacity,
            entityCapacity: _worldOptions.EntityCapacity
        );
        
        var systemServices = new ServiceCollection();
        
        systemServices.AddTransient(typeof(ILogger<>), typeof(Logger<>));
        systemServices.AddSingleton(world);
        systemServices.AddSingleton(_spatialOptions);
        systemServices.AddSingleton(_rootServices.GetRequiredService<IPlayerStagingArea>());
        systemServices.AddSingleton(_rootServices.GetRequiredService<IMapStagingArea>());
        systemServices.AddSingleton(_rootServices.GetRequiredService<ILoggerFactory>());
        
        // MELHORIA: Registro de serviços de rede mais limpo.
        var packetRegistry = new PacketRegistry();
        systemServices.AddSingleton(packetRegistry);
        systemServices.AddSingleton<PacketProcessor>();
        systemServices.AddSingleton<NetworkManager>(sp => new NetworkManager(
            world,
            sp.GetRequiredService<IPlayerIndex>(),
            _networkOptions,
            packetRegistry,
            sp.GetRequiredService<PacketProcessor>(),
            NetworkRole.Server
        ));
        
        // Registro dos sistemas
        systemServices.AddSingleton<NetworkSystem>(); // NOVO: Sistema para PollEvents
        systemServices.AddSingleton<StagingProcessorSystem>();
        systemServices.AddSingleton<EntityIndexSystem>();
        systemServices.AddSingleton<SpatialIndexSystem>();
        systemServices.AddSingleton<EntityFactorySystem>();
        systemServices.AddSingleton<MovementSystem>();
        systemServices.AddSingleton<EntitySaveSystem>();
        systemServices.AddSingleton<EntityDestructorSystem>();
        systemServices.AddSingleton<EntitySnapshotSystem>();
        systemServices.AddSingleton<IPlayerIndex>(sp => sp.GetRequiredService<EntityIndexSystem>());
        systemServices.AddSingleton<IMapIndex>(sp => sp.GetRequiredService<EntityIndexSystem>());
        
        var ecsServiceProvider = systemServices.BuildServiceProvider();
        var pipeline = new Group<float>("SimulationServer Group");
        
        // ADIÇÃO: Adiciona o NetworkSystem no início para processar pacotes primeiro.
        AddSystem<NetworkSystem>(pipeline, ecsServiceProvider);

        // Adiciona sistemas de lógica de jogo
        AddSystem<StagingProcessorSystem>(pipeline, ecsServiceProvider);
        AddSystem<EntityIndexSystem>(pipeline, ecsServiceProvider);
        AddSystem<SpatialIndexSystem>(pipeline, ecsServiceProvider);
        AddSystem<EntityFactorySystem>(pipeline, ecsServiceProvider);
        AddSystem<MovementSystem>(pipeline, ecsServiceProvider);
        
        // --- Loop de Registro de Sincronização ---
        var networkManager = ecsServiceProvider.GetRequiredService<NetworkManager>();
        foreach (var (componentType, options) in _syncRegistrations)
        {
            var registerMethod = typeof(PacketRegistry).GetMethod(nameof(PacketRegistry.Register));
            var genericRegisterMethod = registerMethod!.MakeGenericMethod(componentType);
            genericRegisterMethod.Invoke(packetRegistry, null);
            
            var genericSystemType = typeof(GenericSyncSystem<>);
            var specificSystemType = genericSystemType.MakeGenericType(componentType);

            // CORREÇÃO: Usamos Activator.CreateInstance para passar manualmente o 'options'.
            var systemInstance = (ISystem<float>)Activator.CreateInstance(specificSystemType, world, networkManager, options)!;
            pipeline.Add(systemInstance);
            
            var logger = ecsServiceProvider.GetRequiredService<ILogger<ServerSimulationBuilder>>();
            logger.LogInformation("Sistema de sincronização genérico para {ComponentType} registrado.", componentType.Name);
        }
        
        // Adiciona sistemas de final de quadro
        AddSystem<EntitySaveSystem>(pipeline, ecsServiceProvider);
        AddSystem<EntitySnapshotSystem>(pipeline, ecsServiceProvider);
        AddSystem<EntityDestructorSystem>(pipeline, ecsServiceProvider);
        
        pipeline.Initialize();
        return (pipeline, world);
    }
    
    private void AddSystem<T>(Group<float> group, IServiceProvider provider) where T : class, ISystem<float>
    {
        group.Add(provider.GetRequiredService<T>());
    }
}
