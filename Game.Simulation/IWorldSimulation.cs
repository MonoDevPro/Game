using Arch.Core;
using Game.Contracts;
using Game.Infrastructure.ArchECS.Services.Map;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;

namespace Game.Simulation;

/// <summary>
/// Interface para a simulação do mundo do jogo.
/// Permite abstrair a implementação concreta para facilitar testes e múltiplas implementações.
/// </summary>
public interface IWorldSimulation : IDisposable
{
    /// <summary>
    /// O mundo ECS subjacente.
    /// </summary>
    World World { get; }

    /// <summary>
    /// Tick atual da simulação.
    /// </summary>
    long CurrentTick { get; }

    /// <summary>
    /// Indica se a simulação tem suporte a navegação (pathfinding).
    /// </summary>
    bool HasNavigation { get; }

    /// <summary>
    /// Mapa do mundo (pode ser null se criado sem mapa).
    /// </summary>
    WorldMap? Map { get; }

    /// <summary>
    /// Atualiza a simulação com o delta de tempo em milissegundos.
    /// </summary>
    /// <param name="deltaTimeMs">Tempo desde a última atualização em milissegundos.</param>
    void Update(long deltaTimeMs);

    /// <summary>
    /// Adiciona ou atualiza um jogador no mundo.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <param name="name">Nome do jogador.</param>
    /// <param name="x">Posição X.</param>
    /// <param name="y">Posição Y.</param>
    /// <returns>A entidade criada ou atualizada.</returns>
    Entity UpsertPlayer(int characterId, string name, int x, int y);

    /// <summary>
    /// Remove um jogador do mundo.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <returns>True se o jogador foi removido com sucesso.</returns>
    bool RemovePlayer(int characterId);

    /// <summary>
    /// Verifica se um jogador existe no mundo.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <returns>True se o jogador existe.</returns>
    bool HasPlayer(int characterId);

    /// <summary>
    /// Constrói um snapshot do estado atual do mundo.
    /// </summary>
    /// <returns>Snapshot do mundo para sincronização.</returns>
    WorldSnapshot BuildSnapshot();

    /// <summary>
    /// Solicita movimento de um jogador usando pathfinding.
    /// Requer HasNavigation == true.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <param name="targetX">Posição X de destino.</param>
    /// <param name="targetY">Posição Y de destino.</param>
    /// <param name="targetFloor">Andar de destino.</param>
    /// <param name="flags">Flags de pathfinding.</param>
    /// <returns>True se a requisição foi aceita.</returns>
    bool RequestPlayerMove(int characterId, int targetX, int targetY, int targetFloor = 0,
        PathRequestFlags flags = PathRequestFlags.None);

    /// <summary>
    /// Para o movimento de um jogador.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <returns>True se o movimento foi parado.</returns>
    bool StopPlayerMove(int characterId);

    /// <summary>
    /// Move um jogador diretamente por delta (sem pathfinding).
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <param name="dx">Delta X.</param>
    /// <param name="dy">Delta Y.</param>
    /// <returns>True se o movimento foi aplicado.</returns>
    bool MovePlayerByDelta(int characterId, int dx, int dy);
}
