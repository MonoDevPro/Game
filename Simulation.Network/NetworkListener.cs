using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Simulation.Core.Options;
using Simulation.Core.Ports.Network;
using Simulation.Network.Packet;

namespace Simulation.Network;

public class NetworkListener : INetEventListener, IPeerRepository
    {
        private readonly PacketProcessor _packetProcessor; // ALTERADO: Injetar PacketProcessor
        private readonly NetworkOptions _options;
        private readonly ILogger<NetworkListener> _logger;
        private readonly Dictionary<int, NetPeerAdapter> _connectedPeers = new();

        public event Action<INetPeerAdapter> PeerConnected = delegate { };
        public event Action<INetPeerAdapter> PeerDisconnected = delegate { };

        public NetworkListener(PacketProcessor packetProcessor, NetworkOptions options, ILogger<NetworkListener> logger)
        {
            _packetProcessor = packetProcessor; // ALTERADO
            _options = options;
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
                PeerDisconnected?.Invoke(adapter); // Disparar evento
                _logger.LogInformation("Peer disconnected: {PeerId} - Reason: {Reason}", peer.Id, disconnectInfo.Reason);
            }
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            // Roteamento removido, chama o processador diretamente.
            // O channelNumber é ignorado aqui, pois a lógica está no tipo do pacote.
            _packetProcessor.HandleData(peer, reader, _connectedPeers[peer.Id]);
            reader.Recycle();
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey(_options.ConnectionKey);
        }

        // Outros métodos de INetEventListener (OnNetworkError, etc.) podem permanecer vazios ou com logging.
        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode) { }
        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    }