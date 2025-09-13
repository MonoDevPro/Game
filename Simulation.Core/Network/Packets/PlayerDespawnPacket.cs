using MemoryPack;
using Simulation.Abstractions.Network;

namespace Simulation.Core.Network.Packets;

/// <summary>
/// Pacote enviado para os clientes já no mapa para anunciar a saída de um jogador.
/// </summary>
[MemoryPackable]
public partial record struct PlayerDespawnPacket : IPacket
{
    public int PlayerId { get; set; } // ID do jogador que saiu
}