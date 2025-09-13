using Arch.Core;
using LiteNetLib;
using MemoryPack;
using Simulation.Core.ECS.Shared.Systems.Indexes;

namespace Simulation.Core.Network;

// Delegado para o manipulador de pacotes recebidos.
public delegate void PacketHandler(in NetPacketReader reader, World world, IPlayerIndex playerIndex);

public class PacketRegistry
{
    private readonly Dictionary<Type, byte> _packetTypeToId = new();
    private readonly Dictionary<byte, PacketHandler> _idToHandler = new();
    private byte _nextPacketId = 1;

    public void Register<T>() where T : struct, IEquatable<T>
    {
        if (_packetTypeToId.ContainsKey(typeof(T)))
        {
            return; // Já registrado
        }

        var packetId = _nextPacketId++;
        _packetTypeToId[typeof(T)] = packetId;

        // O handler é o código que sabe como desserializar e aplicar o componente.
        _idToHandler[packetId] = (in NetPacketReader reader, World world, IPlayerIndex playerIndex) =>
        {
            try
            {
                // Desserializa o pacote genérico
                var packet = MemoryPackSerializer.Deserialize<ComponentSyncPacket<T>>(reader.GetRemainingBytesSegment());
                
                // Encontra a entidade e aplica o componente
                if (playerIndex.TryGetPlayerEntity(packet.PlayerId, out var entity))
                    world.AddOrGet(entity, packet.Component);
            }
            catch (Exception ex)
            {
                // Logar o erro - importante para depuração
                Console.WriteLine($"Erro ao processar pacote para {typeof(T).Name}: {ex.Message}");
            }
        };
    }

    public bool TryGetPacketId(Type type, out byte id)
    {
        return _packetTypeToId.TryGetValue(type, out id);
    }

    public bool TryGetHandler(byte id, out PacketHandler? handler)
    {
        return _idToHandler.TryGetValue(id, out handler);
    }
}