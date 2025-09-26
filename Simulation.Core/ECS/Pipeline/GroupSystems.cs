using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Resources;
using Simulation.Core.ECS.Sync;
using Simulation.Core.ECS.Systems;
using Simulation.Core.Options;
using Simulation.Core.Ports.Network;

namespace Simulation.Core.ECS.Pipeline;

public class GroupSystems : ISystem<float>
{
    private readonly World _world;
    private readonly Group<float> _groupSystems;
    private readonly ResourceContext _resources;

    public GroupSystems(World world, ResourceContext resources, AuthorityOptions authorityOptions)
    {
        _world = world;
        _resources = resources;
        
        bool isServer = authorityOptions.Authority == Authority.Server;
        var groupName = isServer ? "ServerSystems" : "ClientSystems";
        _groupSystems = new Group<float>(groupName);
        
        if (isServer)
            AddServerSystems(world);
    }
    
    private void AddServerSystems(World world)
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
    }
    
    private void AddSystem<TSystem>(params object[] args) where TSystem : BaseSystem<World, float>
    {
        var system = Activator.CreateInstance(typeof(TSystem), args) as ISystem<float>;
        _groupSystems.Add(system);
    }
    
    private void AddInboxSync<T>() where T : struct, IEquatable<T>
    {
        var networkInbox = new NetworkInbox<T>();
        _resources.NetworkEndpoint.RegisterHandler<ComponentSyncPacket<T>>((peer, packet) => { networkInbox.Enqueue(packet); });
        AddSystem<NetworkComponentApplySystem<T>>(_world, new NetworkInbox<T>(), _resources.PlayerIndex);
    }
    private void AddOutboxSync<T>(SyncOptions options) where T : struct, IEquatable<T>
    {
        AddSystem<NetworkOutbox<T>>(_world, _resources.NetworkEndpoint, options);
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