using Game.Abstractions.Network;
using Game.Network.Security;
using LiteNetLib;
using MemoryPack;
using Microsoft.Extensions.Logging;

namespace Game.Network.Adapters;

public class PacketProcessor(ILogger<PacketProcessor> logger)
{
    private delegate void UntypedPacketHandler(INetPeerAdapter fromPeer, NetPacketReader reader);
    private readonly Dictionary<ulong, UntypedPacketHandler> _idToHandler = new();

    // Construtor simplificado

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
    }
        
    // ALTERADO: Aceita o INetPeerAdapter
    public void HandleData(NetPeer fromPeer, NetPacketReader dataReader, INetPeerAdapter fromPeerAdapter)
    {
        if (dataReader.AvailableBytes < sizeof(ulong)) return;
        var packetId = dataReader.GetULong();
        if (_idToHandler.TryGetValue(packetId, out var handler))
            handler(fromPeerAdapter, dataReader);
        else
            logger.LogWarning("No handler registered for PacketID {PacketId} from Peer {PeerId}", packetId, fromPeer.Id);
    }

    // O resto da classe (GetPacketId, UnregisterHandler, HashCache) permanece o mesmo.
    public bool UnregisterHandler<T>() where T : struct, IPacket => _idToHandler.Remove(GetHash<T>());
    public ulong GetPacketId<T>() where T : struct, IPacket => GetHash<T>();
    private ulong GetHash<T>() => NetworkHasher<T>.Id;
}