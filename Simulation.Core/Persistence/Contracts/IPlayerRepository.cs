using Simulation.Core.ECS.Components;
using Simulation.Core.Persistence.Models;

namespace Simulation.Core.Persistence.Contracts;

/// <summary>
/// Contrato para o repositório de jogadores, estendendo o repositório genérico
/// com operações específicas para PlayerData.
/// </summary>
public interface IPlayerRepository : IRepositoryAsync<int, PlayerModel>
{
    /// <summary>
    /// Atualiza um PlayerModel na base de dados a partir de um PlayerData vindo do ECS.
    /// </summary>
    Task<bool> UpdateFromDataAsync(PlayerData data, CancellationToken ct = default);

    /// <summary>
    /// Obtém um jogador pelo seu nome (case sensitive simples por enquanto).
    /// </summary>
    Task<PlayerModel?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Cria um novo jogador com valores padrão iniciais.
    /// </summary>
    Task<PlayerModel> CreateWithDefaultsAsync(string name, string passwordHash, CancellationToken ct = default);
}