using Arch.Core;

namespace Simulation.Core.ECS.Indexes.Player;

/// <summary>
/// Define um contrato para um índice que mapeia um ID de jogador para a sua entidade.
/// </summary>
public interface IPlayerIndex
{
    /// <summary>
    /// Obtém a entidade de um jogador pelo seu ID, se existir e estiver viva.
    /// </summary>
    bool TryGetPlayerEntity(int charId, out Entity entity);
    
    /// <summary>
    /// Obtém uma lista de todos os IDs de jogadores presentes num determinado mapa.
    /// </summary>
    IEnumerable<int> GetPlayerIdsInMap(int mapId);
}