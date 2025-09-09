using Arch.Core;
using LiteNetLib;
using LiteNetLib.Utils;
using Simulation.Core.Shared.Network.Contracts;
// CORREÇÃO: Adiciona o using para o namespace do código gerado
using Simulation.Core.Shared.Network.Generated; 

namespace Simulation.Core.Shared.Network;

public class NetworkManager
{
    public NetManager Net { get; }
    public readonly EventBasedNetListener Listener;
    private readonly World _world;

    public NetPeer? ServerConnection { get; private set; }

    public NetworkManager(World world)
    {
        _world = world;
        Listener = new EventBasedNetListener();
        Net = new NetManager(Listener);
    }

    public void StartServer(int port, string connectionKey)
    {
        Net.Start(port);
        Listener.ConnectionRequestEvent += request => request.AcceptIfKey(connectionKey);
        Listener.PeerConnectedEvent += peer => Console.WriteLine($"[Server] Peer connected: {peer.Id}");
        Listener.PeerDisconnectedEvent += (peer, info) => Console.WriteLine($"[Server] Peer disconnected: {peer.Id}");
        Listener.NetworkReceiveEvent += OnReceive;
    }

    public void StartClient(string address, int port, string connectionKey)
    {
        Net.Start();
        Net.Connect(address, port, connectionKey);
        Listener.PeerConnectedEvent += peer =>
        {
            ServerConnection = peer;
            Console.WriteLine($"[Client] Connected to server: {peer.Id}");
        };
        Listener.PeerDisconnectedEvent += (peer, info) => Console.WriteLine($"[Client] Disconnected from server");
        Listener.NetworkReceiveEvent += OnReceive;
    }

    private void OnReceive(NetPeer fromPeer, NetPacketReader dataReader, byte channel, DeliveryMethod deliveryMethod)
    {
        // Esta é a linha 50. Agora ela corresponde à assinatura gerada.
        PacketProcessor.Process(_world, fromPeer, dataReader);
        dataReader.Recycle();
    }

    public void PollEvents()
    {
        Net.PollEvents();
    }

    public void SendToServer<T>(T packet, DeliveryMethod deliveryMethod) where T : IPacket
    {
        if (ServerConnection == null) return;
        Send(ServerConnection, packet, deliveryMethod);
    }
    
    public void Broadcast<T>(T packet, DeliveryMethod deliveryMethod) where T : IPacket
    {
        var writer = GetWriterForPacket(packet);
        Net.SendToAll(writer, deliveryMethod);
    }

    public void Send<T>(NetPeer peer, T packet, DeliveryMethod deliveryMethod) where T : IPacket
    {
        var writer = GetWriterForPacket(packet);
        peer.Send(writer, deliveryMethod);
    }
    
    private NetDataWriter GetWriterForPacket<T>(T packet) where T : IPacket
    {
        var writer = new NetDataWriter();
        // CORREÇÃO: A chamada para PacketFactory agora resolve corretamente devido ao 'using'
        /*var packetType = Simulation.Core.Shared.Network.Generated.p //.GetPacketType(packet);
        var packetBytes = MemoryPackSerializer.Serialize(packet);

        writer.Put((byte)packetType); 
        writer.Put(packetBytes);      */
        return writer;
    }

    public void Stop()
    {
        Net.Stop();
    }
}