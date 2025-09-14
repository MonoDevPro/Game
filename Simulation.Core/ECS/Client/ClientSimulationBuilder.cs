using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Client.Systems;
using Simulation.Core.ECS.Server.Systems;
using Simulation.Core.ECS.Shared.Staging;
using Simulation.Core.ECS.Shared.Systems;
using Simulation.Core.ECS.Shared.Systems.Indexes;
using Simulation.Core.Network;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Client;

public class ClientSimulationBuilder : ISimulationBuilder<float>
{
    private WorldOptions? _worldOptions;
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
        throw new NotImplementedException();
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

    public ISimulationBuilder<float> WithSynchronizedComponent<T>(SyncOptions options) where T : struct, IEquatable<T>
    {
        _syncRegistrations.Add((typeof(T), options));
        return this;
    }

    public (Group<float> Group, World World) Build()
    {
        if (_worldOptions is null || _networkOptions is null || _rootServices is null)
            throw new InvalidOperationException("WorldOptions, RootServices e NetworkOptions devem ser fornecidos.");

        var world = World.Create(
            chunkSizeInBytes: _worldOptions.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: _worldOptions.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: _worldOptions.ArchetypeCapacity,
            entityCapacity: _worldOptions.EntityCapacity
        );
        
        var systemServices = new ServiceCollection();
        
        systemServices.AddTransient(typeof(ILogger<>), typeof(Logger<>));
        systemServices.AddSingleton(world);
        systemServices.AddSingleton(_rootServices.GetRequiredService<ILoggerFactory>());
        systemServices.AddSingleton(_rootServices.GetRequiredService<IPlayerStagingArea>());
        systemServices.AddSingleton(_rootServices.GetRequiredService<IMapStagingArea>());

        var packetRegistry = new PacketRegistry();
        systemServices.AddSingleton(packetRegistry);
        systemServices.AddSingleton<PacketProcessor>();
        systemServices.AddSingleton<NetworkManager>(sp => new NetworkManager(
            world,
            sp.GetRequiredService<IPlayerIndex>(),
            _networkOptions,
            packetRegistry,
            sp.GetRequiredService<PacketProcessor>(),
            NetworkRole.Client // Configurado para o Cliente
        ));
        
        // Sistemas específicos do cliente
        systemServices.AddSingleton<NetworkSystem>();
        systemServices.AddSingleton<StagingProcessorSystem>();
        systemServices.AddSingleton<EntityIndexSystem>(); // O cliente também precisa indexar entidades
        systemServices.AddSingleton<EntityFactorySystem>();
        systemServices.AddSingleton<MovementSystem>();
        systemServices.AddSingleton<RenderSystem>(); // Sistema de renderização
        systemServices.AddSingleton<IPlayerIndex>(sp => sp.GetRequiredService<EntityIndexSystem>());
        
        var ecsServiceProvider = systemServices.BuildServiceProvider();
        var pipeline = new Group<float>("SimulationClient Group");
        
        // A ordem é importante: processar a rede, depois a lógica, depois renderizar.
        AddSystem<NetworkSystem>(pipeline, ecsServiceProvider);
        AddSystem<StagingProcessorSystem>(pipeline, ecsServiceProvider);
        AddSystem<EntityIndexSystem>(pipeline, ecsServiceProvider);
        AddSystem<EntityFactorySystem>(pipeline, ecsServiceProvider);
        AddSystem<MovementSystem>(pipeline, ecsServiceProvider);

        var networkManager = ecsServiceProvider.GetRequiredService<NetworkManager>();
        foreach (var (componentType, options) in _syncRegistrations)
        {
            // O cliente também precisa registrar todos os tipos para poder recebê-los.
            var registerMethod = typeof(PacketRegistry).GetMethod(nameof(PacketRegistry.Register));
            var genericRegisterMethod = registerMethod!.MakeGenericMethod(componentType);
            genericRegisterMethod.Invoke(packetRegistry, null);
            
            // Adiciona o sistema de sincronização. No cliente, ele vai lidar principalmente
            // com o envio de componentes com Authority.Client (ex: InputComponent).
            var genericSystemType = typeof(GenericSyncSystem<>);
            var specificSystemType = genericSystemType.MakeGenericType(componentType);

            var systemInstance = (ISystem<float>)Activator.CreateInstance(specificSystemType, world, networkManager, options)!;
            pipeline.Add(systemInstance);
        }
        
        // Adiciona o sistema de renderização no final da pipeline.
        AddSystem<RenderSystem>(pipeline, ecsServiceProvider);
        
        pipeline.Initialize();
        return (pipeline, world);
    }
    
    private void AddSystem<T>(Group<float> group, IServiceProvider provider) where T : class, ISystem<float>
    {
        group.Add(provider.GetRequiredService<T>());
    }
}