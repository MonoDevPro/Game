using Arch.Core;
using Simulation.Core.ECS.Sync;
using Simulation.Core.Ports.Network;

namespace Simulation.Core.ECS.Resource;

public class PlayerNetResource(World world, PlayerIndexResource playerIndexResource, INetworkManager networkManager)
{
    private readonly HashSet<Type> _registeredSyncs = [];
    
    public void SendToPlayer<T>(int playerId, in T message, NetworkChannel channel = NetworkChannel.Simulation,  NetworkDeliveryMethod delivery = NetworkDeliveryMethod.ReliableOrdered) where T : struct, IPacket
    {
        networkManager.SendToPeerId(playerId, message, channel, delivery);
    }
    
    public void SendToServer<T>(in T message, NetworkChannel channel = NetworkChannel.Simulation, NetworkDeliveryMethod delivery = NetworkDeliveryMethod.ReliableOrdered) where T : struct, IPacket
    {
        networkManager.SendToServer(message, channel, delivery);
    }
    
    public void BroadcastToAll<T>(in T message, NetworkChannel channel = NetworkChannel.Simulation, NetworkDeliveryMethod delivery = NetworkDeliveryMethod.ReliableOrdered) where T : struct, IPacket
    {
        networkManager.SendToAll(message, channel, delivery);
    }
    
    public NetworkComponentApplySystem<T> RegisterComponentUpdate<T>() where T : struct, IEquatable<T>
    {
        if (!_registeredSyncs.Add(typeof(T)))
            throw new InvalidOperationException($"Component sync for type {typeof(T)} is already registered.");

        var networkInbox = new NetworkInbox<T>();
        networkManager.RegisterPacketHandler<ComponentSyncPacket<T>>((peer, packet) => { networkInbox.Enqueue(packet); });
        return new NetworkComponentApplySystem<T>(world, networkInbox, playerIndexResource);
    }
    
    public NetworkOutbox<T> RegisterComponentPost<T>(SyncOptions options) where T : struct, IEquatable<T>
    {
        return new NetworkOutbox<T>(world, this, options);
    }
    
    private void UnregisterComponentSync<T>() where T : struct, IEquatable<T>
    {
        if (!_registeredSyncs.Remove(typeof(T)))
            throw new InvalidOperationException($"Component sync for type {typeof(T)} is not registered.");
        
        _registeredSyncs.Remove(typeof(T));
        networkManager.UnregisterPacketHandler<ComponentSyncPacket<T>>();
    }
}