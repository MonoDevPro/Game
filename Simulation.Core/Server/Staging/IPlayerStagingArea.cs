using Simulation.Core.Shared.Templates;

namespace Simulation.Core.Server.Staging;

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
    bool TryDequeueLogin(out PlayerData? template);
    
    void StageLeave(int charId);
    bool TryDequeueLeave(out int charId);

    void StageSave(PlayerData data);
}