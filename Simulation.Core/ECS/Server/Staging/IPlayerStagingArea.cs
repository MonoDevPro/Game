using Simulation.Core.ECS.Shared.Data;

namespace Simulation.Core.ECS.Server.Staging;

/// <summary>
/// Uma área de transferência thread-safe para manter os dados de jogadores que foram
/// carregados do banco de dados e estão aguardando para serem adicionados ao mundo ECS.
/// </summary>
public interface IPlayerStagingArea
{
    /// <summary>
    /// Coloca os dados de um jogador na fila de espera para entrar no ECS.
    /// </summary>
    void StageLogin(PlayerData data);
    /// <summary>
    /// Tenta retirar os dados de um jogador da fila.
    /// </summary>
    bool TryDequeueLogin(out PlayerData data);
    
    void StageLeave(int playerId);
    bool TryDequeueLeave(out int playerId);

    void StageSave(PlayerData data);
}