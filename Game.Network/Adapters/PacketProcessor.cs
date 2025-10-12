using System.Net;
using Game.Network.Abstractions;
using Game.Network.Security;
using LiteNetLib;
using MemoryPack;
using Microsoft.Extensions.Logging;

namespace Game.Network.Adapters;

/// <summary>
/// Processador de pacotes com suporte a Connected e Unconnected.
/// Usa hashing para identificação rápida de tipos de pacote.
/// Autor: MonoDevPro
/// Data: 2025-10-12 05:45:41
/// </summary>
public class PacketProcessor(ILogger<PacketProcessor> logger)
{
    // ========== DELEGATES ==========
    
    private delegate void UntypedPacketHandler(INetPeerAdapter fromPeer, NetPacketReader reader);
    private delegate void UntypedUnconnectedHandler(IPEndPoint remoteEndPoint, NetPacketReader reader);

    // ========== CONNECTED HANDLERS ==========
    
    private readonly Dictionary<ulong, UntypedPacketHandler> _idToHandler = new();

    /// <summary>
    /// Registra handler de pacote CONNECTED (de NetPeer).
    /// </summary>
    public void RegisterHandler<T>(PacketHandler<T> handler) where T : struct, IPacket
    {
        var hash = GetHash<T>();
        if (_idToHandler.ContainsKey(hash))
        {
            logger.LogWarning("Handler for packet {PacketType} is already registered.", typeof(T).Name);
            return;
        }
            
        _idToHandler[hash] = (fromPeer, reader) =>
        {
            try
            {
                var packet = MemoryPackSerializer.Deserialize<T>(reader.GetRemainingBytesSegment());
                handler(fromPeer, packet);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deserializing or handling packet {PacketType}", typeof(T).Name);
            }
        };
        
        logger.LogDebug("Registered CONNECTED handler for {PacketType} (Hash: {Hash})", typeof(T).Name, hash);
    }

    /// <summary>
    /// Desregistra handler de pacote CONNECTED.
    /// </summary>
    public bool UnregisterHandler<T>() where T : struct, IPacket
    {
        var hash = GetHash<T>();
        var result = _idToHandler.Remove(hash);
        
        if (result)
        {
            logger.LogDebug("Unregistered CONNECTED handler for {PacketType}", typeof(T).Name);
        }
        
        return result;
    }

    /// <summary>
    /// Processa pacote CONNECTED (de NetPeer).
    /// </summary>
    public void HandleData(NetPeer fromPeer, NetPacketReader dataReader, INetPeerAdapter fromPeerAdapter)
    {
        if (dataReader.AvailableBytes < sizeof(ulong))
        {
            logger.LogWarning("Received packet with insufficient data from Peer {PeerId}", fromPeer.Id);
            return;
        }

        var packetId = dataReader.GetULong();
        
        if (_idToHandler.TryGetValue(packetId, out var handler))
        {
            handler(fromPeerAdapter, dataReader);
        }
        else
        {
            logger.LogWarning(
                "No CONNECTED handler registered for PacketID {PacketId} from Peer {PeerId}", 
                packetId, 
                fromPeer.Id
            );
        }
    }

    // ========== UNCONNECTED HANDLERS ==========
    
    private readonly Dictionary<ulong, UntypedUnconnectedHandler> _idToUnconnectedHandler = new();

    /// <summary>
    /// ✅ Registra handler de pacote UNCONNECTED (sem NetPeer).
    /// </summary>
    public void RegisterUnconnectedHandler<T>(UnconnectedPacketHandler<T> handler) where T : struct, IPacket
    {
        var hash = GetHash<T>();
        if (_idToUnconnectedHandler.ContainsKey(hash))
        {
            logger.LogWarning("Unconnected handler for packet {PacketType} is already registered.", typeof(T).Name);
            return;
        }
            
        _idToUnconnectedHandler[hash] = (remoteEndPoint, reader) =>
        {
            try
            {
                var packet = MemoryPackSerializer.Deserialize<T>(reader.GetRemainingBytesSegment());
                handler(remoteEndPoint, packet);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex, 
                    "Error deserializing or handling UNCONNECTED packet {PacketType} from {EndPoint}", 
                    typeof(T).Name, 
                    remoteEndPoint
                );
            }
        };
        
        logger.LogDebug("Registered UNCONNECTED handler for {PacketType} (Hash: {Hash})", typeof(T).Name, hash);
    }

    /// <summary>
    /// ✅ Desregistra handler de pacote UNCONNECTED.
    /// </summary>
    public bool UnregisterUnconnectedHandler<T>() where T : struct, IPacket
    {
        var hash = GetHash<T>();
        var result = _idToUnconnectedHandler.Remove(hash);
        
        if (result)
        {
            logger.LogDebug("Unregistered UNCONNECTED handler for {PacketType}", typeof(T).Name);
        }
        
        return result;
    }

    /// <summary>
    /// ✅ Processa pacote UNCONNECTED (sem NetPeer).
    /// </summary>
    public void HandleUnconnectedData(IPEndPoint remoteEndPoint, NetPacketReader dataReader)
    {
        if (dataReader.AvailableBytes < sizeof(ulong))
        {
            logger.LogWarning("Received UNCONNECTED packet with insufficient data from {EndPoint}", remoteEndPoint);
            return;
        }

        var packetId = dataReader.GetULong();
        
        if (_idToUnconnectedHandler.TryGetValue(packetId, out var handler))
        {
            handler(remoteEndPoint, dataReader);
        }
        else
        {
            logger.LogWarning(
                "No UNCONNECTED handler registered for PacketID {PacketId} from {EndPoint}", 
                packetId, 
                remoteEndPoint
            );
        }
    }

    // ========== UTILITIES ==========

    /// <summary>
    /// Obtém o ID (hash) de um tipo de pacote.
    /// </summary>
    public ulong GetPacketId<T>() where T : struct, IPacket => GetHash<T>();

    /// <summary>
    /// Obtém o hash do tipo de pacote usando NetworkHasher.
    /// </summary>
    private ulong GetHash<T>() => NetworkHasher<T>.Id;
}