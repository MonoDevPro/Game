using System.Collections.Concurrent;
using Arch.Core;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;
using Simulation.Abstractions.Network;
using Simulation.Core.ECS.Server.Systems;
using Simulation.Core.ECS.Server.Systems.Indexes;
using Simulation.Core.Options;
using Simulation.Generated.Network;

namespace Simulation.Core.Network;

public class NetworkManager
{
    public NetManager Net { get; }
    public readonly EventBasedNetListener Listener;
    private readonly World _world;
    private readonly NetworkOptions _options;
    
    // Mapeamento interno de PlayerId para a conexão NetPeer.
    private readonly ConcurrentDictionary<int, NetPeer> _peersById = new();

    public NetPeer? ServerConnection { get; private set; }
    
    private readonly IPlayerIndex _playerIndex; // Adicionar esta linha

    public NetworkManager(World world, IPlayerIndex playerIndex, NetworkOptions options) // Modificar construtor
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
            _peersById[peer.Id] = peer; // Adiciona ao nosso índice
            DebugPacketProcessor.LogConnectionEvent($"Peer connected: {peer.Id}", true);
        };
        Listener.PeerDisconnectedEvent += (peer, info) => 
        {
            _peersById.TryRemove(peer.Id, out _); // Remove do nosso índice
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
    
    /// <summary>
    /// Envia um pacote para um jogador específico usando o seu PlayerId.
    /// </summary>
    public void SendTo(int playerId, IPacket packet, DeliveryMethod deliveryMethod)
    {
        if (_peersById.TryGetValue(playerId, out var peer))
        {
            var writer = GetWriterForPacket(packet);
            peer.Send(writer, deliveryMethod);
        }
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
    
    /// <summary>
    /// Envia um pacote para todos os jogadores num mapa específico.
    /// </summary>
    public void BroadcastToAllInMap(int mapId, IPacket packet, DeliveryMethod deliveryMethod)
    {
        var writer = GetWriterForPacket(packet);
        var playerIds = _playerIndex.GetPlayerIdsInMap(mapId);
        foreach (var playerId in playerIds)
        {
            if (_peersById.TryGetValue(playerId, out var peer))
            {
                peer.Send(writer, deliveryMethod);
            }
        }
    }
    
    /// <summary>
    /// Envia um pacote para todos os jogadores num mapa, exceto um.
    /// </summary>
    public void BroadcastToOthersInMap(int excludedPlayerId, int mapId, IPacket packet, DeliveryMethod deliveryMethod)
    {
        var writer = GetWriterForPacket(packet);
        var playerIds = _playerIndex.GetPlayerIdsInMap(mapId);
        foreach (var playerId in playerIds)
        {
            if (playerId == excludedPlayerId) continue;
            
            if (_peersById.TryGetValue(playerId, out var peer))
            {
                peer.Send(writer, deliveryMethod);
            }
        }
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