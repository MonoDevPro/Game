using Arch.Core;

namespace Simulation.Core.Ports;

/// <summary>
/// Define um contrato para um índice que mapeia um ID de jogador para a sua entidade.
/// </summary>
public interface IPlayerIndex
{
    /// <summary>
    /// Obtém a entidade de um jogador pelo seu ID, se existir e estiver viva.
    /// </summary>
    bool TryGetPlayerEntity(int charId, out Entity entity);
}