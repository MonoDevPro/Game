using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Client.Systems;
using Simulation.Core.ECS.Server;
using Simulation.Core.ECS.Shared.Staging;
using Simulation.Core.ECS.Shared.Systems;
using Simulation.Core.ECS.Shared.Systems.Indexes;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Client;

public class ClientSimulationBuilder : ISimulationBuilder<float>
{
    private WorldOptions? _worldOptions;
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
        if (_worldOptions is null || _rootServices is null)
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
        
        systemServices.AddSingleton(_rootServices.GetRequiredService<INetworkManager>());
        var endpoint = _rootServices
            .GetRequiredService<IChannelProcessorFactory>()
            .CreateOrGet(NetworkChannel.Simulation);
        systemServices.AddSingleton<IChannelEndpoint>(endpoint);

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

        // Registro dos pacotes de autenticação
        // Pacotes de resposta não precisam de handler no servidor.
        // IDS serão registrados pelo AuthService externo antes da construção se necessário.
        foreach (var (componentType, options) in _syncRegistrations)
        {
            var genericSystemType = typeof(GenericSyncSystem<>);
            var specificSystemType = genericSystemType.MakeGenericType(componentType);

            // CORREÇÃO: Usamos Activator.CreateInstance para passar manualmente o 'options'.
            var systemInstance = (ISystem<float>)Activator.CreateInstance(specificSystemType, world, endpoint, options)!;
            pipeline.Add(systemInstance);
            
            var logger = ecsServiceProvider.GetRequiredService<ILogger<ClientSimulationBuilder>>();
            logger.LogInformation("Sistema de sincronização genérico para {ComponentType} registrado.", componentType.Name);
        }
        
        
        foreach (var (componentType, options) in _syncRegistrations)
        {
            var realType = typeof(ComponentSyncPacket<>);
            var specificType = realType.MakeGenericType(componentType);
            
            // O cliente também precisa registrar todos os tipos para poder recebê-los.
            var registerMethod = typeof(IChannelEndpoint).GetMethod(nameof(IChannelEndpoint.RegisterHandler));
            var genericRegisterMethod = registerMethod!.MakeGenericMethod(specificType);
            genericRegisterMethod.Invoke(endpoint, null);
            
            // Adiciona o sistema de sincronização. No cliente, ele vai lidar principalmente
            // com o envio de componentes com Authority.Client (ex: InputComponent).
            var genericSystemType = typeof(GenericSyncSystem<>);
            var specificSystemType = genericSystemType.MakeGenericType(componentType);

            var systemInstance = (ISystem<float>)Activator.CreateInstance(specificSystemType, world, endpoint, options)!;
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