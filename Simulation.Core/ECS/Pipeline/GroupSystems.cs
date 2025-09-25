using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Services;
using Simulation.Core.ECS.Sync;
using Simulation.Core.ECS.Systems;
using Simulation.Core.ECS.Systems.Resources;
using Simulation.Core.Options;
using Simulation.Core.Ports.ECS;
using Simulation.Core.Ports.Network;

namespace Simulation.Core.ECS.Pipeline;

public class GroupSystems : ISystem<float>
{
    private readonly Group<float> _groupSystems;
    private readonly IServiceProvider _internalProvider;

    public GroupSystems(IServiceProvider externalProvider, IChannelEndpoint endpoint, AuthorityOptions authorityOptions, WorldOptions worldOptions)
    {
        bool isServer = authorityOptions.Authority == Authority.Server;
        var groupName = isServer ? "ServerSystems" : "ClientSystems";
        
        var world = World.Create(
            chunkSizeInBytes: worldOptions.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: worldOptions.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: worldOptions.ArchetypeCapacity,
            entityCapacity: worldOptions.EntityCapacity);
        
        _groupSystems = new Group<float>(groupName);
        
        var internalCollection = new ServiceCollection();
        
        if (isServer)
            RegisterServerResources(world, externalProvider, internalCollection);
            
        _internalProvider = internalCollection.BuildServiceProvider();
        
        if (isServer)
            AddServerSystems(world, endpoint);
        
    }
    
    private void RegisterServerResources(World world, IServiceProvider externalProvider, IServiceCollection internalCollection)
    {
        var loggerFactory = externalProvider.GetRequiredService<ILoggerFactory>();
        
        var worldSaver = externalProvider.GetRequiredService<IWorldSaver>();
        var mapService = externalProvider.GetRequiredService<MapService>();
        
        var playerIndex = new PlayerIndexResource(world);
        var playerSave = new PlayerSaveResource(world, worldSaver);
        var spatialIndex = new SpatialIndexResource(mapService);
        var playerFactory = new PlayerFactoryResource(world, playerIndex, spatialIndex);
        
        internalCollection.AddSingleton(loggerFactory);
        internalCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        internalCollection.AddSingleton<IPlayerIndex>(playerIndex);
        internalCollection.AddSingleton<ISpatialIndex>(spatialIndex);
        
        internalCollection.AddSingleton(playerIndex);
        internalCollection.AddSingleton(playerFactory);
        internalCollection.AddSingleton(playerSave);
        internalCollection.AddSingleton(spatialIndex);
    }
    
    private void AddServerSystems(World world, IChannelEndpoint endpoint)
    {
        // Receive systems (inbox)
        AddInboxSync<Input>();
        
        // Game logic systems
        AddSystem<MovementSystem>(world);
        
        // Send systems (outbox)
        AddOutboxSync<State>(new SyncOptions(Authority.Server, SyncFrequency.OnChange, SyncTarget.Broadcast, NetworkDeliveryMethod.ReliableOrdered, 0));
        AddOutboxSync<Position>(new SyncOptions(Authority.Server, SyncFrequency.OnChange, SyncTarget.Broadcast, NetworkDeliveryMethod.ReliableOrdered, 0));
        AddOutboxSync<Direction>(new SyncOptions(Authority.Server, SyncFrequency.OnChange, SyncTarget.Broadcast, NetworkDeliveryMethod.ReliableOrdered, 0));
        AddOutboxSync<Health>(new SyncOptions(Authority.Server, SyncFrequency.OnChange, SyncTarget.Broadcast, NetworkDeliveryMethod.ReliableOrdered, 0));

        return;
        
        void AddInboxSync<T>() where T : struct, IEquatable<T>
        {
            var networkInbox = new NetworkInbox<T>();
            endpoint.RegisterHandler<ComponentSyncPacket<T>>((peer, packet) => { networkInbox.Enqueue(packet); });
            AddSystem<NetworkComponentApplySystem<T>>(world, new NetworkInbox<T>());
        }
        
        void AddSystem<TSystem>(params object[] args) where TSystem : BaseSystem<World, float>
        {
            var system = ActivatorUtilities.CreateInstance(_internalProvider, typeof(TSystem), args) as ISystem<float>;
            _groupSystems.Add(system);
        }
        
        void AddOutboxSync<T>(SyncOptions options) where T : struct, IEquatable<T>
        {
            AddSystem<NetworkOutbox<T>>(world, endpoint, options);
        }
        
    }

    /// <summary>
    /// Inicializa todos os sistemas registados no grupo.
    /// </summary>
    public void Initialize()
    {
        _groupSystems.Initialize();
    }

    /// <summary>
    /// O método BeforeUpdate é delegado para o grupo interno.
    /// </summary>
    public void BeforeUpdate(in float t)
    {
        _groupSystems.BeforeUpdate(in t);
    }

    /// <summary>
    /// O método Update agora orquestra o ciclo de vida completo para todos os sistemas.
    /// Chama BeforeUpdate, Update e AfterUpdate em sequência.
    /// </summary>
    public void Update(in float t)
    {
        // 1. Chama BeforeUpdate para todos os sistemas.
        _groupSystems.BeforeUpdate(in t);
        
        // 2. Chama Update para todos os sistemas.
        _groupSystems.Update(in t);
        
        // 3. Chama AfterUpdate para todos os sistemas.
        _groupSystems.AfterUpdate(in t);
    }
    
    /// <summary>
    /// O método AfterUpdate é delegado para o grupo interno.
    /// </summary>
    public void AfterUpdate(in float t)
    {
        _groupSystems.AfterUpdate(in t);
    }

    /// <summary>
    /// Liberta os recursos de todos os sistemas registados.
    /// </summary>
    public void Dispose()
    {
        _groupSystems.Dispose();
    }
}