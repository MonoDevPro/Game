using System.Collections.Concurrent;
using Arch.Core;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;
using Simulation.Abstractions.Network;
using Simulation.Core.ECS.Shared.Systems.Indexes;
using Simulation.Core.Options;

namespace Simulation.Core.Network;

[MemoryPackable]
public partial struct ComponentSyncPacket<T> : IPacket where T : struct, IEquatable<T>
{
    // Usamos PlayerId em vez de EntityId para associar ao jogador correto.
    public int PlayerId { get; set; }
    public T Component { get; set; }
}

public enum NetworkRole
{
    Server,
    Client
}

public class NetworkManager
{
    public NetManager Net { get; }
    public readonly EventBasedNetListener Listener;
    private readonly World _world;
    private readonly NetworkOptions _options;
    private readonly PacketRegistry _packetRegistry; // Adicione este campo
    private readonly PacketProcessor _packetProcessor;
    private readonly NetDataWriter _writer = new(); // Adicione este campo
    private readonly NetworkRole _role;
    
    // Mapeamento interno de PlayerId para a conexão NetPeer.
    private readonly ConcurrentDictionary<int, NetPeer> _peersById = new();

    public NetPeer? ServerConnection { get; private set; }
    
    private readonly IPlayerIndex _playerIndex;

    public NetworkManager(World world, IPlayerIndex playerIndex, NetworkOptions options, PacketRegistry packetRegistry, PacketProcessor processor, NetworkRole role)
    {
        _world = world;
        _playerIndex = playerIndex;
        _options = options;
        Listener = new EventBasedNetListener();
        Net = new NetManager(Listener)
        {
            DisconnectTimeout = options.DisconnectTimeoutMs
        };
        _packetRegistry = packetRegistry;
        _packetProcessor = processor;
        _role = role;
    }
    
    public void Initialize()
    {
        if (_role == NetworkRole.Server)
            StartServer();
        else
            StartClient();
    }

    public void StartServer()
    {
        Net.Start(_options.ServerPort);
        Listener.ConnectionRequestEvent += request => request.AcceptIfKey(_options.ConnectionKey);
        Listener.PeerConnectedEvent += peer => {
            _peersById[peer.Id] = peer; // Adiciona ao nosso índice
        };
        Listener.PeerDisconnectedEvent += (peer, info) => 
        {
            _peersById.TryRemove(peer.Id, out _); // Remove do nosso índice
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
        };
        Listener.PeerDisconnectedEvent += (peer, info) => {
            Console.WriteLine($"[Client] Disconnected from server");
        };
        Listener.NetworkReceiveEvent += OnReceive;
    }

    private void OnReceive(NetPeer fromPeer, NetPacketReader dataReader, byte channel, DeliveryMethod deliveryMethod)
    {
        // Use DebugPacketProcessor which wraps the generated PacketProcessor with debug functionality
        _packetProcessor.Process(dataReader, _world, _playerIndex);
        dataReader.Recycle();
    }

    public void PollEvents()
    {
        Net.PollEvents();
    }
    
    // NOVO MÉTODO para enviar componentes genéricos
    public void SendComponent<T>(int playerId, T component, DeliveryMethod deliveryMethod) where T : struct, IEquatable<T>
    {
        if (!_packetRegistry.TryGetPacketId(typeof(T), out var packetId))
        {
            // O componente não foi registrado para sincronização.
            return;
        }

        var packet = new ComponentSyncPacket<T>
        {
            PlayerId = playerId,
            Component = component
        };

        _writer.Reset();
        _writer.Put(packetId); // 1. Escreve o ID do tipo de pacote

        var payload = MemoryPackSerializer.Serialize(packet);
        _writer.Put(payload); // 2. Escreve o pacote serializado

        Broadcast(_writer, deliveryMethod);
    }
    
    public void SendTo<T>(int playerId, T packet, DeliveryMethod deliveryMethod) where T : struct, IEquatable<T>
    {
        if (!_packetRegistry.TryGetPacketId(typeof(T), out var packetId))
        {
            // O componente não foi registrado para sincronização.
            return;
        }
        _writer.Reset();
        _writer.Put(packetId); // 1. Escreve o ID do tipo de pacote

        var payload = MemoryPackSerializer.Serialize(packet);
        _writer.Put(payload); // 2. Escreve o pacote serializado
        SendTo(playerId, _writer, deliveryMethod);
    }
    
    // Modifique os métodos de envio para aceitar NetDataWriter
    public void Broadcast(NetDataWriter writer, DeliveryMethod deliveryMethod)
    {
        Net?.SendToAll(writer, deliveryMethod);
    }
    
    /// <summary>
    /// Envia um pacote para um jogador específico usando o seu PlayerId.
    /// </summary>
    public void SendTo(int playerId, NetDataWriter writer, DeliveryMethod deliveryMethod)
    {
        if (_peersById.TryGetValue(playerId, out var peer))
        {
            peer.Send(writer, deliveryMethod);
        }
    }

    /// <summary>
    /// Envia um pacote para todos os jogadores num mapa específico.
    /// </summary>
    public void BroadcastToAllInMap<T>(int mapId, T packet, DeliveryMethod deliveryMethod) where T : struct, IEquatable<T>
    {
        if (!_packetRegistry.TryGetPacketId(typeof(T), out var packetId))
        {
            // O componente não foi registrado para sincronização.
            return;
        }
        _writer.Reset();
        _writer.Put(packetId); // 1. Escreve o ID do tipo de pacote
        
        var payload = MemoryPackSerializer.Serialize(packet);
        _writer.Put(payload); // 2. Escreve o pacote serializado
        
        var playerIds = _playerIndex.GetPlayerIdsInMap(mapId);
        foreach (var playerId in playerIds)
        {
            if (_peersById.TryGetValue(playerId, out var peer))
            {
                peer.Send(_writer, deliveryMethod);
            }
        }
    }
    
    /// <summary>
    /// Envia um pacote para todos os jogadores num mapa, exceto um.
    /// </summary>
    public void BroadcastToOthersInMap<T>(int excludedPlayerId, int mapId, T packet, DeliveryMethod deliveryMethod) where T : struct, IEquatable<T>
    {
        if (!_packetRegistry.TryGetPacketId(typeof(T), out var packetId))
        {
            // O componente não foi registrado para sincronização.
            return;
        }
        _writer.Reset();
        _writer.Put(packetId); // 1. Escreve o ID do tipo de pacote
        
        var payload = MemoryPackSerializer.Serialize(packet);
        _writer.Put(payload); // 2. Escreve o pacote serializado
        
        var playerIds = _playerIndex.GetPlayerIdsInMap(mapId);
        foreach (var playerId in playerIds)
        {
            if (playerId == excludedPlayerId) continue;
            
            if (_peersById.TryGetValue(playerId, out var peer))
            {
                peer.Send(_writer, deliveryMethod);
            }
        }
    }

    public void Stop()
    {
        Net.Stop();
    }
}