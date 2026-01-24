using Game.Contracts;
using Game.Infrastructure.Serialization;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Game.Infrastructure.LiteNetLib;

public sealed class NetServer : IDisposable
{
    private readonly EventBasedNetListener _listener;
    private readonly NetManager _manager;
    private readonly IMessageSerializer _serializer;

    public event Action<NetPeer, Envelope>? EnvelopeReceived;
    public event Action<NetPeer>? PeerConnected;
    public event Action<NetPeer, DisconnectInfo>? PeerDisconnected;

    public NetServer(IMessageSerializer serializer)
    {
        _serializer = serializer;
        _listener = new EventBasedNetListener();
        _manager = new NetManager(_listener)
        {
            AutoRecycle = true
        };

        _listener.ConnectionRequestEvent += request => request.AcceptIfKey("GameServer");
        _listener.PeerConnectedEvent += peer => PeerConnected?.Invoke(peer);
        _listener.PeerDisconnectedEvent += (peer, info) => PeerDisconnected?.Invoke(peer, info);
        _listener.NetworkReceiveEvent += OnReceive;
    }

    public void Start(int port)
    {
        _manager.Start(port);
    }

    public void PollEvents()
    {
        _manager.PollEvents();
    }

    public void Stop()
    {
        _manager.Stop();
    }

    public void Send(NetPeer peer, Envelope envelope, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        var data = _serializer.Serialize(envelope);
        var writer = new NetDataWriter();
        writer.Put(data);
        peer.Send(writer, deliveryMethod);
    }

    public void SendToAll(Envelope envelope, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        foreach (var peer in _manager.ConnectedPeerList)
        {
            Send(peer, envelope, deliveryMethod);
        }
    }

    private void OnReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        var payload = reader.GetRemainingBytes();
        var envelope = _serializer.Deserialize(payload);
        EnvelopeReceived?.Invoke(peer, envelope);
        reader.Recycle();
    }

    public void Dispose()
    {
        _manager.Stop();
    }
}
