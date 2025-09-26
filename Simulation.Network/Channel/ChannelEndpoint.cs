using LiteNetLib;
using Microsoft.Extensions.Logging;
using Simulation.Core.Ports.Network;
using Simulation.Network.Packet;

namespace Simulation.Network.Channel;

internal class ChannelEndpoint(
    NetworkChannel channel,
    PacketProcessor processor,
    PacketSender sender,
    NetworkListener listener,
    ILogger<ChannelEndpoint> logger)
    : IChannelEndpoint
{
    public void RegisterHandler<T>(PacketHandler<T> handler) where T : struct, IPacket
        => processor.RegisterHandler(handler);
    
    public void Handle(NetPeer fromPeer, NetPacketReader dataReader)
        => processor.HandleData(fromPeer, dataReader);
    
    public bool IsRegisteredHandler<T>() where T : struct, IPacket
        => processor.IsRegistered<T>();

    public bool UnregisterHandler<T>() where T : struct, IPacket
    {
        return processor.UnregisterHandler<T>();
    }
    
    public void SendToPeer<T>(INetPeerAdapter peer, T packet, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket
        => sender.SendToPeer(listener.ConnectedPeers[peer.Id].Peer, channel, packet, deliveryMethod.ToLite());
    
    public void SendToPeerId<T>(int peerId, T packet, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket
        => sender.SendToPeerId(peerId, channel, packet, deliveryMethod.ToLite());
    
    public void SendToServer<T>(T packet, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket
        => sender.SendToServer(channel, packet, deliveryMethod.ToLite());
    
    public void SendToAll<T>(T packet, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket
        => sender.SendToAll(channel, packet, deliveryMethod.ToLite());
    
    public void SendToAllExcept<T>(INetPeerAdapter excludePeer, T packet, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket
        => sender.SendToAllExcept(listener.ConnectedPeers[excludePeer.Id].Peer, channel, packet, deliveryMethod.ToLite());
}