using System.Net;
using Game.ECS.Shared.Services.Network;
using LiteNetLib;
using Microsoft.Extensions.Logging;

namespace Game.Network.Adapters;

public class NetworkManager : INetworkManager, IDisposable
{
    private readonly NetworkOptions _netOptions;
    private readonly NetManager _net;
    private readonly NetworkListener _listener;
    private readonly PacketSender _packetSender;
    private readonly PacketProcessor _packetProcessor;

    public NetworkManager(ILogger<NetworkManager> logger,
        NetworkOptions netOptions,
        NetManager net,
        NetworkListener listener,
        PacketSender packetSender,
        PacketProcessor packetProcessor)
    {
        _netOptions = netOptions;
        _net = net;
        _listener = listener;
        _packetSender = packetSender;
        _packetProcessor = packetProcessor;
        
        listener.PeerConnected += peer => OnPeerConnected?.Invoke(peer);
        listener.PeerDisconnected += peer => OnPeerDisconnected?.Invoke(peer);
    }

    public event Action<INetPeerAdapter> OnPeerConnected = delegate { };
    public event Action<INetPeerAdapter> OnPeerDisconnected = delegate { };

    public bool IsRunning => _net.IsRunning;
    public IPeerRepository Peers => _listener;
    
    public void Initialize()
    {
        if (_netOptions.IsServer)
        {
            _net.Start(_netOptions.ServerPort);
        }
        else
        {
            _net.Start();
        }
    }
        
    public void ConnectToServer()
    {
        if (!_netOptions.IsServer)
        {
            if (!_net.IsRunning) _net.Start();
            _net.Connect(_netOptions.ServerAddress, _netOptions.ServerPort, _netOptions.ConnectionKey);
        }
    }

    public void Stop() => _net.Stop();

    public void PollEvents() => _net.PollEvents();

    public void RegisterPacketHandler<T>(PacketHandler<T> handler) 
        => _packetProcessor.RegisterHandler(handler);

    public bool UnregisterPacketHandler<T>() 
        => _packetProcessor.UnregisterHandler<T>();

    public void RegisterUnconnectedPacketHandler<T>(UnconnectedPacketHandler<T> handler) 
    {
        _packetProcessor.RegisterUnconnectedHandler(handler);
    }

    public bool UnregisterUnconnectedPacketHandler<T>() 
    {
        return _packetProcessor.UnregisterUnconnectedHandler<T>();
    }

    public void SendToServer<T>(T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) 
        => _packetSender.SendToServer(ref packet, channel, deliveryMethod.ToLite());

    public void SendToPeer<T>(INetPeerAdapter peer, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) 
    {
        _packetSender.SendToPeerId(peer.Id, ref packet, channel, deliveryMethod.ToLite());
    }
        
    public void SendUnconnected<T>(IPEndPoint endPoint, T packet) 
        => _packetSender.SendUnconnected(endPoint, ref packet);
        
    public void SendToPeerId<T>(int peerId, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) 
        => _packetSender.SendToPeerId(peerId, ref packet, channel, deliveryMethod.ToLite());
        
    public void SendToAll<T>(T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) 
        => _packetSender.SendToAll(ref packet, channel, deliveryMethod.ToLite());
        
    public void SendToAllExcept<T>(INetPeerAdapter excludePeer, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) 
    {
        if (excludePeer is NetPeerAdapter adapter)
            _packetSender.SendToAllExcept(adapter.Peer, ref packet, channel, deliveryMethod.ToLite());
    }
    
    public void SendToAllExcept<T>(int excludePeerId, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) 
    {
        if (!_net.TryGetPeerById(excludePeerId, out var peer))
            return;
        
        _packetSender.SendToAllExcept(peer, ref packet, channel, deliveryMethod.ToLite());
    }

    public void Dispose()
    {
        Stop();
    }
}