using LiteNetLib;
using MemoryPack;
using Microsoft.Extensions.Logging;
using Simulation.Core.Ports.Network;

namespace Simulation.Network.Packet;

public class PacketProcessor
    {
        private delegate void UntypedPacketHandler(INetPeerAdapter fromPeer, NetPacketReader reader);
        private readonly Dictionary<ulong, UntypedPacketHandler> _idToHandler = new();
        private readonly ILogger<PacketProcessor> _logger;

        public PacketProcessor(ILogger<PacketProcessor> logger) // Construtor simplificado
        {
            _logger = logger;
        }

        public void RegisterHandler<T>(PacketHandler<T> handler) where T : struct, IPacket
        {
            var hash = GetHash<T>();
            if (_idToHandler.ContainsKey(hash))
            {
                _logger.LogWarning("Handler for packet {PacketType} is already registered.", typeof(T).Name);
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
                    _logger.LogError(ex, "Error deserializing or handling packet {PacketType}", typeof(T).Name);
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
                _logger.LogWarning("No handler registered for PacketID {PacketId} from Peer {PeerId}", packetId, fromPeer.Id);
        }

        // O resto da classe (GetPacketId, UnregisterHandler, HashCache) permanece o mesmo.
        public bool UnregisterHandler<T>() where T : struct, IPacket => _idToHandler.Remove(GetHash<T>());
        public ulong GetPacketId<T>() where T : struct, IPacket => GetHash<T>();
        private ulong GetHash<T>() => HashCache<T>.Id;
    
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