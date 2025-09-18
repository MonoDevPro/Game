using LiteNetLib;
using MemoryPack;
using Microsoft.Extensions.Logging;
using Simulation.Core.Network.Contracts;

namespace Simulation.Network.Packet;

public class PacketProcessor(NetworkListener listener, ILogger<PacketProcessor>? logger)
{
    private delegate void UntypedPacketHandler(INetPeerAdapter fromPeer, NetPacketReader reader);

    private readonly Dictionary<ulong, UntypedPacketHandler> _idToHandler = new();

    public void RegisterHandler<T>(PacketHandler<T> handler) where T : struct, IPacket
    {
        var hash = GetHash<T>();
        
        if (_idToHandler.ContainsKey(hash))
        {
            logger?.LogWarning("O handler para o pacote {PacketType} já foi registado.", typeof(T).Name);
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
                logger?.LogError(ex, "Erro ao desserializar ou manipular o pacote {PacketType}", typeof(T).Name);
            }
        };
    }

    public void HandleData(NetPeer fromPeer, NetPacketReader dataReader)
    {
        if (dataReader.AvailableBytes < 1) return;
        var packetId = dataReader.GetULong();
        if (_idToHandler.TryGetValue(packetId, out var handler))
            handler(listener.ConnectedPeers[fromPeer.Id], dataReader);
        else
            logger?.LogWarning("Nenhum handler registado para o PacketID {PacketId}", packetId);
    }
    
    public ulong GetPacketId<T>() where T : struct, IPacket
    {
        var hash = GetHash<T>();
        return hash;
    }

    public bool IsRegistered<T>() where T : struct, IPacket
    {
        var hash = GetHash<T>();
        return _idToHandler.ContainsKey(hash);
    }
    
    private ulong GetHash<T>() => PacketProcessor.HashCache<T>.Id;
    
    private static class HashCache<T>
    {
        public static readonly ulong Id;
        static HashCache()
        {
            ulong num1 = 14695981039346656037;
            foreach (ulong num2 in typeof (T).ToString())
                num1 = (num1 ^ num2) * 1099511628211UL /*0x0100000001B3*/;
            Id = num1;
        }
    } 
}