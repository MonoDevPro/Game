using System.Linq.Expressions;
using System.Reflection;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Indexes.Player;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.ECS.Staging;
using Simulation.Core.ECS.Systems;
using Simulation.Core.ECS.Systems.Client;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Builders;

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
        systemServices.AddSingleton(_rootServices.GetRequiredService<IWorldStaging>());
        
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
        
        // registra pipeline e provider no container
        var pipeline = new PipelineSystems(ecsServiceProvider, isServer: false);
        
        // Registro dos pacotes de autenticação
        // Pacotes de resposta não precisam de handler no servidor.
        // IDS serão registrados pelo AuthService externo antes da construção se necessário.
        var index = ecsServiceProvider.GetRequiredService<IPlayerIndex>();
        foreach (var (componentType, options) in _syncRegistrations)
        {
            var genericSystemType = typeof(GenericSyncSystem<>);
            var specificSystemType = genericSystemType.MakeGenericType(componentType);

            // CORREÇÃO: Usamos Activator.CreateInstance para passar manualmente o 'options'.
            var systemInstance = (ISystem<float>)Activator.CreateInstance(specificSystemType, world, endpoint, index, options)!;
            pipeline.Add(systemInstance);
            
            var logger = ecsServiceProvider.GetRequiredService<ILogger<ClientSimulationBuilder>>();
            logger.LogInformation("Sistema de sincronização genérico para {ComponentType} registrado.", componentType.Name);
        }
        
        // Sistemas de sync genéricos dinamicamente adicionados abaixo
        
        pipeline.Initialize();
        return (pipeline, world);
    }
    
    private void AddSystem<T>(Group<float> group, IServiceProvider provider) where T : class, ISystem<float>
    {
        group.Add(provider.GetRequiredService<T>());
    }
    
    static void EnsurePacketTypeIsValid(Type packetType)
    {
        if (packetType == null) throw new ArgumentNullException(nameof(packetType));
        if (!packetType.IsValueType) throw new ArgumentException("T must be a struct (value type)", nameof(packetType));
        if (!typeof(IPacket).IsAssignableFrom(packetType)) throw new ArgumentException("T must implement IPacket", nameof(packetType));
    }
    // Exemplo genérico para invocar SendToPeerId<T>(int peerId, T packet, NetworkDeliveryMethod m)
    public static void InvokeSendToPeerId(IChannelEndpoint endpoint, Type packetType, int peerId, object packet, NetworkDeliveryMethod method)
    {
        EnsurePacketTypeIsValid(packetType);

        var mi = typeof(IChannelEndpoint).GetMethod(nameof(IChannelEndpoint.SendToPeerId)) 
                 ?? throw new InvalidOperationException("Method not found");
        var gen = mi.MakeGenericMethod(packetType);
        // packet deve ser um boxed value type compatível com packetType
        gen.Invoke(endpoint, new object[] { peerId, packet, method });
    }
    // Exemplo para consultar IsRegisteredHandler<T>()
    public static bool InvokeIsRegisteredHandler(IChannelEndpoint endpoint, Type packetType)
    {
        EnsurePacketTypeIsValid(packetType);

        var mi = typeof(IChannelEndpoint).GetMethod(nameof(IChannelEndpoint.IsRegisteredHandler))
                 ?? throw new InvalidOperationException("Method not found");
        var gen = mi.MakeGenericMethod(packetType);
        var result = gen.Invoke(endpoint, null);
        return result is true;
    }
    
    public static class ChannelReflectionHelpers
    {
        // Cria um PacketHandler<T> que chama targetMethod (por exemplo: void OnPacket(INetPeerAdapter peer, object pkt))
        // targetInstance pode ser null se o método for estático.
        public static Delegate CreatePacketHandlerDelegate(Type packetType, object targetInstance, MethodInfo targetMethod)
        {
            EnsurePacketTypeIsValid(packetType);
            if (targetMethod == null) throw new ArgumentNullException(nameof(targetMethod));

            // tipo do delegate: PacketHandler<T>
            var packetHandlerType = typeof(PacketHandler<>).MakeGenericType(packetType);

            // parâmetros do delegate: (INetPeerAdapter peer, T packet)
            var peerParam = Expression.Parameter(typeof(INetPeerAdapter), "peer");
            var packetParam = Expression.Parameter(packetType, "packet");

            // precisamos adaptar packetParam para o parâmetro do targetMethod (ex: object)
            // Supondo targetMethod signature: void M(INetPeerAdapter, object) ou (NetPeer, object) etc.
            var targetParams = targetMethod.GetParameters();
            if (targetParams.Length != 2)
                throw new ArgumentException("targetMethod deve ter 2 parâmetros (INetPeerAdapter, object) ou equivalente");

            // converte packetParam para o tipo esperado pelo targetMethod (ex: object)
            Expression convertedPacket = packetParam;
            var expectedPacketType = targetParams[1].ParameterType;
            if (expectedPacketType != packetType)
            {
                convertedPacket = Expression.Convert(packetParam, expectedPacketType);
            }

            // converte peerParam se necessário
            Expression peerArg = peerParam;
            var expectedPeerType = targetParams[0].ParameterType;
            if (expectedPeerType != typeof(INetPeerAdapter))
                peerArg = Expression.Convert(peerParam, expectedPeerType);

            // construir chamada ao método alvo
            Expression call;
            if (targetMethod.IsStatic)
            {
                call = Expression.Call(targetMethod, peerArg, convertedPacket);
            }
            else
            {
                var instance = Expression.Constant(targetInstance ?? throw new ArgumentNullException(nameof(targetInstance)), targetInstance.GetType());
                // se o tipo do instance não for exato, converter
                Expression instanceExp = instance;
                if (targetMethod.DeclaringType != null && targetMethod.DeclaringType != instance.Type)
                    instanceExp = Expression.Convert(instance, targetMethod.DeclaringType);
                call = Expression.Call(instanceExp, targetMethod, peerArg, convertedPacket);
            }

            // lambda do tipo PacketHandler<T>
            var lambda = Expression.Lambda(packetHandlerType, call, peerParam, packetParam);
            return lambda.Compile(); // Delegate do tipo PacketHandler<T>
        }

        // Usa o delegate criado para chamar RegisterHandler<T> por reflection
        public static void RegisterHandlerDynamic(IChannelEndpoint endpoint, Type packetType, object targetInstance, MethodInfo targetMethod)
        {
            EnsurePacketTypeIsValid(packetType);

            var handlerDelegate = CreatePacketHandlerDelegate(packetType, targetInstance, targetMethod);

            var mi = typeof(IChannelEndpoint).GetMethod(nameof(IChannelEndpoint.RegisterHandler))
                     ?? throw new InvalidOperationException("RegisterHandler method not found");
            var gen = mi.MakeGenericMethod(packetType);
            gen.Invoke(endpoint, new object[] { handlerDelegate });
        }
    }

}