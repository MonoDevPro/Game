namespace Game.Abstractions.Network;

public interface IPacket;

public enum NetworkChannel : byte
{
    Simulation = 0,
}

public delegate void PacketHandler<in T>(INetPeerAdapter fromPeer, T packet) 
    where T : IPacket;

public enum NetworkDeliveryMethod : byte
{
    /// <summary>
    /// Reliable. Packets won't be dropped, won't be duplicated, can arrive without order.
    /// </summary>
    ReliableUnordered,
    /// <summary>
    /// Unreliable. Packets can be dropped, won't be duplicated, will arrive in order.
    /// </summary>
    Sequenced,
    /// <summary>
    /// Reliable and ordered. Packets won't be dropped, won't be duplicated, will arrive in order.
    /// </summary>
    ReliableOrdered,
    /// <summary>
    /// Reliable only last packet. Packets can be dropped (except the last one), won't be duplicated, will arrive in order.
    /// Cannot be fragmented
    /// </summary>
    ReliableSequenced,
    /// <summary>
    /// Unreliable. Packets can be dropped, can be duplicated, can arrive without order.
    /// </summary>
    Unreliable,
}

public interface INetworkManager
{
    // Eventos para conectar/desconectar peers.
    event Action<INetPeerAdapter> OnPeerConnected;
    event Action<INetPeerAdapter> OnPeerDisconnected;

    bool IsRunning { get; }
    IPeerRepository Peers { get; }

    // Métodos de ciclo de vida.
    void Start();
    void Stop();
    void PollEvents();

    // Registro de handlers de pacotes.
    void RegisterPacketHandler<T>(PacketHandler<T> handler) where T : struct, IPacket;
    bool UnregisterPacketHandler<T>() where T : struct, IPacket;

    // Métodos de envio de pacotes.
    void SendToServer<T>(T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket;
    void SendToPeer<T>(INetPeerAdapter peer, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket;
    void SendToPeerId<T>(int peerId, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket;
    void SendToAll<T>(T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket;
    void SendToAllExcept<T>(INetPeerAdapter excludePeer, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket;
}
