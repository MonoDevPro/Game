using LiteNetLib;
using MemoryPack;
using Microsoft.Extensions.Logging;
using Simulation.Core.Network.Contracts;

namespace Simulation.Network.Packet;

public class PacketProcessor(NetworkListener listener, ILogger<PacketProcessor>? logger)
{
    private delegate void UntypedPacketHandler(INetPeerAdapter fromPeer, NetPacketReader reader);

    private readonly Dictionary<Type, byte> _typeToId = new();
    private readonly Dictionary<byte, UntypedPacketHandler> _idToHandler = new();
    private byte _nextId = 1;

    public void RegisterHandler<T>(PacketHandler<T> handler) where T : struct, IPacket
    {
        var packetType = typeof(T);
        if (_typeToId.ContainsKey(packetType)) return;

        var packetId = _nextId++;
        _typeToId[packetType] = packetId;

        _idToHandler[packetId] = (fromPeer, reader) =>
        {
            try
            {
                var packet = MemoryPackSerializer.Deserialize<T>(reader.GetRemainingBytesSegment());
                handler(fromPeer, packet);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Erro ao desserializar ou manipular o pacote {PacketType}", packetType.Name);
            }
        };
    }

    public void HandleData(NetPeer fromPeer, NetPacketReader dataReader)
    {
        if (dataReader.AvailableBytes < 1) return;
        var packetId = dataReader.GetByte();
        if (_idToHandler.TryGetValue(packetId, out var handler))
            handler(listener.ConnectedPeers[fromPeer.Id], dataReader);
        else
            logger?.LogWarning("Nenhum handler registado para o PacketID {PacketId}", packetId);
    }
    
    public byte GetPacketId(Type type)
    {
        if (_typeToId.TryGetValue(type, out var id))
            return id;
        logger?.LogCritical($"O tipo de pacote {type.Name} não foi registado.");
        throw new InvalidOperationException($"O tipo de pacote {type.Name} não foi registado.");
    }

    public bool IsRegistered<T>() where T : struct, IPacket
    {
        return _typeToId.ContainsKey(typeof(T));
    }
}