using System.Net;
using System.Net.Sockets;
using Game.Abstractions.Network;
using Game.Network.Security;
using LiteNetLib;
using Microsoft.Extensions.Logging;

namespace Game.Network.Adapters;

public class NetworkListener : INetEventListener, IPeerRepository
    {
        private readonly PacketProcessor _packetProcessor; // ALTERADO: Injetar PacketProcessor
        private readonly ILogger<NetworkListener> _logger;
        private readonly Dictionary<int, NetPeerAdapter> _connectedPeers = new();

        private readonly NetworkSecurity _networkSecurity;

        public event Action<INetPeerAdapter> PeerConnected = delegate { };
        public event Action<INetPeerAdapter> PeerDisconnected = delegate { };

        public NetworkListener(
            PacketProcessor packetProcessor, 
            NetworkSecurity security,
            ILogger<NetworkListener> logger)
        {
            _packetProcessor = packetProcessor; // ALTERADO
            _networkSecurity = security;
            _logger = logger;
        }
        
        // ... métodos de IPeerRepository (GetAllPeers, TryGetPeer, etc.) permanecem os mesmos ...
        public IEnumerable<INetPeerAdapter> GetAllPeers() => _connectedPeers.Values;
        public bool TryGetPeer(int peerId, out INetPeerAdapter? peer)
        {
            if (_connectedPeers.TryGetValue(peerId, out var netPeer)) { peer = netPeer; return true; }
            peer = null; return false;
        }
        public int PeerCount => _connectedPeers.Count;

        // Implementação de INetEventListener
        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            var adapter = new NetPeerAdapter(peer);
            _connectedPeers[peer.Id] = adapter;
            PeerConnected?.Invoke(adapter); // Disparar evento
            _logger.LogInformation("Peer connected: {PeerId} - {EndPoint}", peer.Id, peer.Address);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (_connectedPeers.Remove(peer.Id, out var adapter))
            {
                _networkSecurity.RemovePeer(peer);
                PeerDisconnected?.Invoke(adapter); // Disparar evento
                _logger.LogInformation("Peer disconnected: {PeerId} - Reason: {Reason}", peer.Id, disconnectInfo.Reason);
            }
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            if (!_networkSecurity.ValidateMessage(peer, reader))
            {
                _logger.LogWarning("Invalid message from Peer {PeerId}. Disconnecting.", peer.Id);
                reader.Recycle();
                return;
            }
            _packetProcessor.HandleData(peer, reader, _connectedPeers[peer.Id]);
            reader.Recycle();
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            _networkSecurity.ValidateConnectionRequest(request);
        }

        // Outros métodos de INetEventListener (OnNetworkError, etc.) podem permanecer vazios ou com logging.
        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode) { }
        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    }