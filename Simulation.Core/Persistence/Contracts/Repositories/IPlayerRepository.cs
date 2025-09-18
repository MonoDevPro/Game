using Simulation.Core.ECS.Components;

namespace Simulation.Core.Persistence.Contracts.Repositories;

/// <summary>
/// Contrato para o repositório de jogadores, estendendo o repositório genérico
/// com operações específicas para PlayerData.
/// </summary>
public interface IPlayerRepository
{
    Task<bool> UpdateFromDataAsync(PlayerData data, CancellationToken ct = default);
    Task<PlayerData?> GetPlayerByName(string name, CancellationToken ct = default);
    Task<PlayerData?> GetPlayerById(int id, CancellationToken ct = default);
}