using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;
using Simulation.Network.Channel;

namespace Simulation.Network;

public class NetworkListener(ChannelRouter router, NetworkOptions options) : INetEventListener, IPeerRepository
{
    private readonly Dictionary<int, NetPeerAdapter> _connectedPeers = new Dictionary<int, NetPeerAdapter>();
    internal IReadOnlyDictionary<int, NetPeerAdapter> ConnectedPeers => _connectedPeers;

    internal INetEventListener NetEventListener => this;

    void INetEventListener.OnPeerConnected(NetPeer peer)
        => _connectedPeers[peer.Id] = new NetPeerAdapter(peer);

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        => _connectedPeers.Remove(peer.Id);

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    { }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        router.Handle(peer, reader, channelNumber);
        reader.Recycle();
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) 
    { }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency) 
    { }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey(options.ConnectionKey);
    }

    public bool TryGetPeer(int peerId, out INetPeerAdapter? peer)
    {
        if (_connectedPeers.TryGetValue(peerId, out var netPeer))
        {
            peer = netPeer;
            return true;
        }
        peer = null;
        return false;
    }

    public IEnumerable<INetPeerAdapter> GetAllPeers()
    {
        return _connectedPeers.Values;
    }

    public int PeerCount => _connectedPeers.Count;
}