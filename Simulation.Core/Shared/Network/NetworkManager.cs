using Arch.Core;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;
using Simulation.Abstractions.Network;
using Simulation.Core.Server.Systems;
using Simulation.Core.Shared.Options;
using Simulation.Generated.Network; 

namespace Simulation.Core.Shared.Network;

public class NetworkManager
{
    public NetManager Net { get; }
    public readonly EventBasedNetListener Listener;
    private readonly World _world;
    private readonly NetworkOptions _options;

    public NetPeer? ServerConnection { get; private set; }
    
    private readonly EntityIndexSystem _playerIndex; // Adicionar esta linha

    public NetworkManager(World world, EntityIndexSystem playerIndex, NetworkOptions options) // Modificar construtor
    {
        _world = world;
        _playerIndex = playerIndex; // Adicionar esta linha
        _options = options;
        Listener = new EventBasedNetListener();
        Net = new NetManager(Listener);
    }

    public void InitializeDebug(DebugOptions debugOptions)
    {
        DebugPacketProcessor.Initialize(debugOptions);
    }

    public void StartServer()
    {
        Net.Start(_options.ServerPort);
        Listener.ConnectionRequestEvent += request => request.AcceptIfKey(_options.ConnectionKey);
        Listener.PeerConnectedEvent += peer => {
            Console.WriteLine($"[Server] Peer connected: {peer.Id}");
            DebugPacketProcessor.LogConnectionEvent($"Peer connected: {peer.Id}", true);
        };
        Listener.PeerDisconnectedEvent += (peer, info) => {
            Console.WriteLine($"[Server] Peer disconnected: {peer.Id}");
            DebugPacketProcessor.LogConnectionEvent($"Peer disconnected: {peer.Id}", false);
        };
        Listener.NetworkReceiveEvent += OnReceive;
    }

    public void StartClient()
    {
        Net.Start();
        Net.Connect(_options.ServerAddress, _options.ServerPort, _options.ConnectionKey);
        Listener.PeerConnectedEvent += peer =>
        {
            ServerConnection = peer;
            Console.WriteLine($"[Client] Connected to server: {peer.Id}");
            DebugPacketProcessor.LogConnectionEvent($"Client connected to server: {peer.Id}", true);
        };
        Listener.PeerDisconnectedEvent += (peer, info) => {
            Console.WriteLine($"[Client] Disconnected from server");
            DebugPacketProcessor.LogConnectionEvent("Client disconnected from server", false);
        };
        Listener.NetworkReceiveEvent += OnReceive;
    }

    private void OnReceive(NetPeer fromPeer, NetPacketReader dataReader, byte channel, DeliveryMethod deliveryMethod)
    {
        // Use DebugPacketProcessor which wraps the generated PacketProcessor with debug functionality
        DebugPacketProcessor.Process(_world, _playerIndex, dataReader);
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
        
        // Enhanced debug logging is now handled in DebugPacketProcessor for received packets
        // For sent packets, we keep simple logging for now
        Console.WriteLine($"Sending packet of type {packet.GetType().Name} to peer {peer.Id}");
    }
    
    private NetDataWriter GetWriterForPacket<T>(T packet) where T : IPacket
    {
        var writer = new NetDataWriter();
        var packetType = PacketFactory.GetPacketType(packet);
        var packetBytes = MemoryPackSerializer.Serialize(packet);

        writer.Put((byte)packetType); 
        writer.Put(packetBytes);      
        return writer;
    }

    public void Stop()
    {
        Net.Stop();
    }

    public void LogPacketStatistics()
    {
        DebugPacketProcessor.LogStatistics();
    }

    public void ClearPacketStatistics()
    {
        DebugPacketProcessor.ClearStatistics();
    }
}