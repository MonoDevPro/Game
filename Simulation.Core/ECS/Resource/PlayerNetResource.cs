using Arch.Core;
using Simulation.Core.ECS.Sync;
using Simulation.Core.Options;
using Simulation.Core.Ports.Network;

namespace Simulation.Core.ECS.Resource;

public class PlayerNetResource(World world, PlayerIndexResource playerIndexResource, IChannelEndpoint channelEndpoint)
{
    private readonly HashSet<Type> _registeredSyncs = [];
    
    public void SendToPlayer<T>(int playerId, in T message, NetworkDeliveryMethod delivery) where T : struct, IPacket
    {
        channelEndpoint.SendToPeerId(playerId, message, delivery);
    }
    
    public void SendToServer<T>(in T message, NetworkDeliveryMethod delivery) where T : struct, IPacket
    {
        channelEndpoint.SendToServer(message, delivery);
    }
    
    public void BroadcastToAll<T>(in T message, NetworkDeliveryMethod delivery) where T : struct, IPacket
    {
        channelEndpoint.SendToAll(message, delivery);
    }
    
    public NetworkComponentApplySystem<T> RegisterComponentUpdate<T>() where T : struct, IEquatable<T>
    {
        if (!_registeredSyncs.Add(typeof(T)))
            throw new InvalidOperationException($"Component sync for type {typeof(T)} is already registered.");

        var networkInbox = new NetworkInbox<T>();
        channelEndpoint.RegisterHandler<ComponentSyncPacket<T>>((peer, packet) => { networkInbox.Enqueue(packet); });
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
        channelEndpoint.UnregisterHandler<ComponentSyncPacket<T>>();
    }
}