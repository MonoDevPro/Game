using System.Net;
using System.Net.Sockets;
using Game.Network.Abstractions;
using LiteNetLib;
using Microsoft.Extensions.Logging;

namespace Game.Network.Adapters;

public class NetworkListener(
    string connectionKey,
    PacketProcessor packetProcessor,
    ILogger<NetworkListener> logger)
    : INetEventListener, IPeerRepository
{
    private readonly Dictionary<int, NetPeerAdapter> _connectedPeers = new();

    public event Action<INetPeerAdapter> PeerConnected = delegate { };
        public event Action<INetPeerAdapter> PeerDisconnected = delegate { };

        public IEnumerable<INetPeerAdapter> GetAllPeers() => _connectedPeers.Values;
        public bool TryGetPeer(int peerId, out INetPeerAdapter? peer)
        {
            if (_connectedPeers.TryGetValue(peerId, out var netPeer)) { peer = netPeer; return true; }
            peer = null; return false;
        }
        public int PeerCount => _connectedPeers.Count;

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            var adapter = new NetPeerAdapter(peer);
            _connectedPeers[peer.Id] = adapter;
            PeerConnected?.Invoke(adapter); // Disparar evento
            logger.LogInformation("Peer connected: {PeerId} - {EndPoint}", peer.Id, peer.Address);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (_connectedPeers.Remove(peer.Id, out var adapter))
            {
                PeerDisconnected?.Invoke(adapter); // Disparar evento
                logger.LogInformation("Peer disconnected: {PeerId} - Reason: {Reason}", peer.Id, disconnectInfo.Reason);
            }
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            packetProcessor.HandleData(peer, reader, _connectedPeers[peer.Id]);
            reader.Recycle();
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            if (request.AcceptIfKey(connectionKey) is null)
                logger.LogWarning("Invalid connection key from {RemoteEndPoint}", request.RemoteEndPoint);
        }
        
        // ✅ UNCONNECTED EVENTS
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            logger.LogDebug(
                "Received UNCONNECTED packet from {EndPoint} (Type: {MessageType})", 
                remoteEndPoint, 
                messageType
            );

            // ✅ Usa HandleUnconnectedData para pacotes UNCONNECTED
            packetProcessor.HandleUnconnectedData(remoteEndPoint, reader);
            reader.Recycle();
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode) { }
        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    }