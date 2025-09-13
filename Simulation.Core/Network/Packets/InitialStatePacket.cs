using MemoryPack;
using Simulation.Abstractions.Network;
using Simulation.Core.ECS.Shared.Data;

namespace Simulation.Core.Network.Packets;

/// <summary>
/// Pacote enviado UMA VEZ para um jogador que entra no jogo.
/// Contém o estado inicial de todas as entidades que ele precisa de conhecer.
/// </summary>
[MemoryPackable]
public partial record struct InitialStatePacket : IPacket
{
    public int PlayerId { get; set; } // ID do jogador que recebe este pacote
    
    /// <summary>
    /// O estado completo do mapa em que o jogador está a entrar.
    /// </summary>
    public MapData CurrentMap { get; set; }

    /// <summary>
    /// Um array com os dados de todos os outros jogadores já presentes no mapa.
    /// </summary>
    public PlayerData[] OtherPlayers { get; set; }
}