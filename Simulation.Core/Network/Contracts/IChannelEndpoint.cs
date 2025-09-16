namespace Simulation.Core.Network.Contracts;

public interface IChannelEndpoint
{
    void RegisterHandler<T>(PacketHandler<T> handler) where T : struct, IPacket;
    bool IsRegisteredHandler<T>() where T : struct, IPacket;
    void SendToPeerId<T>(int peerId, T packet, NetworkDeliveryMethod networkDeliveryMethod) where T : struct, IPacket;
    void SendToServer<T>(T packet, NetworkDeliveryMethod networkDeliveryMethod) where T : struct, IPacket;
    void SendToPeer<T>(INetPeerAdapter peer, T packet, NetworkDeliveryMethod networkDeliveryMethod) where T : struct, IPacket;
    void SendToAll<T>(T packet, NetworkDeliveryMethod networkDeliveryMethod) where T : struct, IPacket;
    void SendToAllExcept<T>(INetPeerAdapter excludePeer, T packet, NetworkDeliveryMethod networkDeliveryMethod) where T : struct, IPacket;
}