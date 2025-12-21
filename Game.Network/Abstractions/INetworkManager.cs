using System.Net;

namespace Game.Network.Abstractions;

public enum NetworkChannel : byte
{
    Menu,
    Simulation,
    Chat,
}

public delegate void PacketHandler<T>(INetPeerAdapter fromPeer, ref T packet) ;

// ✅ Delegate para handlers unconnected
public delegate void UnconnectedPacketHandler<T>(IPEndPoint remoteEndPoint, ref T packet) ;

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
    void Initialize();
    void ConnectToServer();
    void Stop();
    void PollEvents();

    // Registro de handlers de pacotes.
    void RegisterPacketHandler<T>(PacketHandler<T> handler) ;
    bool UnregisterPacketHandler<T>() ;
    
    // ✅ UNCONNECTED PACKETS
    void RegisterUnconnectedPacketHandler<T>(UnconnectedPacketHandler<T> handler) ;
    bool UnregisterUnconnectedPacketHandler<T>() ;

    // Métodos de envio de pacotes.
    void SendToServer<T>(T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) ;
    void SendToPeer<T>(INetPeerAdapter peer, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) ;
    void SendToPeerId<T>(int peerId, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) ;
    void SendToAll<T>(T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) ;
    void SendToAllExcept<T>(INetPeerAdapter excludePeer, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) ;
    void SendToAllExcept<T>(int excludePeerId, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) ;
    void SendUnconnected<T>(IPEndPoint endPoint, T packet) ;
}
