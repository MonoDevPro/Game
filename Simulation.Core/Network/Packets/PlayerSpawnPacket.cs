using MemoryPack;
using Simulation.Core.ECS.Shared.Data;

namespace Simulation.Core.Network.Packets;

/// <summary>
/// Pacote enviado para os clientes já no mapa para anunciar a chegada de um novo jogador.
/// </summary>
[MemoryPackable]
public partial record struct PlayerSpawnPacket : IPacket
{
    public int PlayerId { get; set; } // ID do jogador que está a chegar

    /// <summary>
    /// Os dados do jogador que acabou de entrar.
    /// </summary>
    public PlayerData Data { get; set; }
}