using LiteNetLib;
using Simulation.Network.Packet;
using Microsoft.Extensions.Logging;
using Simulation.Core.Ports.Network;
using GameWeb.Application.Common.Options;
using Simulation.Core;

namespace Simulation.Network;

public class NetworkManager : INetworkManager, IDisposable
    {
        private readonly NetManager _net;
        private readonly NetworkListener _listener;
        private readonly PacketSender _packetSender;
        private readonly PacketProcessor _packetProcessor;
        
        private readonly NetworkOptions _netOptions;
        private readonly AuthorityOptions _authorityOptions;

        public event Action<INetPeerAdapter> OnPeerConnected = delegate { };
        public event Action<INetPeerAdapter> OnPeerDisconnected = delegate { };

        public Authority Authority => _authorityOptions.Authority;
        public bool IsRunning => _net.IsRunning;
        public IPeerRepository Peers => _listener;

        public NetworkManager(NetworkOptions netOptions, AuthorityOptions authorityOptions, ILoggerFactory loggerFactory)
        {
            _netOptions = netOptions;
            _authorityOptions = authorityOptions;
            
            // 1. Criar um PacketProcessor central.
            _packetProcessor = new PacketProcessor(loggerFactory.CreateLogger<PacketProcessor>());

            // 2. Criar o Listener, que agora depende diretamente do PacketProcessor.
            _listener = new NetworkListener(_packetProcessor, netOptions, loggerFactory.CreateLogger<NetworkListener>());
            
            // Assinar eventos do listener para retransmiti-los.
            _listener.PeerConnected += peer => OnPeerConnected?.Invoke(peer);
            _listener.PeerDisconnected += peer => OnPeerDisconnected?.Invoke(peer);

            // 3. Configurar o NetManager.
            _net = new NetManager(_listener)
            {
                DisconnectTimeout = netOptions.DisconnectTimeoutMs,
                ChannelsCount = (byte)Enum.GetValues(typeof(NetworkChannel)).Length
            };
            
            // 4. Criar um PacketSender central.
            _packetSender = new PacketSender(_net, _packetProcessor);
        }

        public void Start()
        {
            if (Authority == Authority.Server)
                _net.Start(_netOptions.ServerPort);
            else
            {
                _net.Start();
                _net.Connect(_netOptions.ServerAddress, _netOptions.ServerPort, _netOptions.ConnectionKey);
            }
        }

        public void Stop() => _net.Stop();

        public void PollEvents() => _net.PollEvents();

        public void RegisterPacketHandler<T>(PacketHandler<T> handler) where T : struct, IPacket
            => _packetProcessor.RegisterHandler(handler);

        public bool UnregisterPacketHandler<T>() where T : struct, IPacket
            => _packetProcessor.UnregisterHandler<T>();

        public void SendToServer<T>(T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket
            => _packetSender.SendToServer(packet, channel, deliveryMethod.ToLite());

        public void SendToPeer<T>(INetPeerAdapter peer, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket
        {
            if (peer is NetPeerAdapter adapter)
                _packetSender.SendToPeer(adapter.Peer, packet, channel, deliveryMethod.ToLite());
        }
        
        public void SendToPeerId<T>(int peerId, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket
            => _packetSender.SendToPeerId(peerId, packet, channel, deliveryMethod.ToLite());
        
        public void SendToAll<T>(T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket
            => _packetSender.SendToAll(packet, channel, deliveryMethod.ToLite());
        
        public void SendToAllExcept<T>(INetPeerAdapter excludePeer, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket
        {
            if (excludePeer is NetPeerAdapter adapter)
                _packetSender.SendToAllExcept(adapter.Peer, packet, channel, deliveryMethod.ToLite());
        }

        public void Dispose()
        {
            Stop();
        }
    }