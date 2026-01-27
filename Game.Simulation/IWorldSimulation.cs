using Arch.Core;
using Game.Contracts;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Map;

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
    /// <param name="teamId">ID do time.</param>
    /// <param name="name">Nome do jogador.</param>
    /// <param name="x">Posição X.</param>
    /// <param name="y">Posição Y.</param>
    /// <param name="floor">Andar.</param>
    /// <param name="dirX">Direção X.</param>
    /// <param name="dirY">Direção Y.</param>
    /// <param name="vocation">Vocação.</param>
    /// <param name="level">Nível.</param>
    /// <param name="experience">Experiência.</param>
    /// <param name="strength">Força.</param>
    /// <param name="endurance">Resistência.</param>
    /// <param name="agility">Agilidade.</param>
    /// <param name="intelligence">Inteligência.</param>
    /// <param name="willpower">Força de vontade.</param>
    /// <param name="healthPoints">Pontos de vida.</param>
    /// <param name="manaPoints">Pontos de mana.</param>
    /// <returns>A entidade criada ou atualizada.</returns>
    Entity UpsertPlayer(int characterId, int teamId, string name, int x, int y, int floor, int dirX, int dirY,
        byte vocation, int level, long experience,
        int strength, int endurance, int agility, int intelligence, int willpower,
        int healthPoints, int manaPoints);

    /// <summary>
    /// Remove um jogador do mundo.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <returns>True se o jogador foi removido com sucesso.</returns>
    bool RemovePlayer(int characterId);

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
    bool RequestPlayerMove(int characterId, int targetX, int targetY, int targetFloor,
        PathRequestFlags flags = PathRequestFlags.None);

    bool RequestPlayerMoveDelta(int characterId, int deltaX, int deltaY,
        PathRequestFlags flags = PathRequestFlags.None);

    bool RequestBasicAttack(int characterId, int dirX, int dirY);

    /// <summary>
    /// Para o movimento de um jogador.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <returns>True se o movimento foi parado.</returns>
    bool StopPlayerMove(int characterId);
}
