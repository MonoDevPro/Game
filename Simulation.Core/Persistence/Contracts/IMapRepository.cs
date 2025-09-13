using Simulation.Core.ECS.Shared.Data;
using Simulation.Core.Persistence.Models;

namespace Simulation.Core.Persistence.Contracts;

/// <summary>
/// Contrato para o repositório de mapas, estendendo o repositório genérico
/// com operações específicas para MapData.
/// </summary>
public interface IMapRepository : IRepositoryAsync<int, MapModel>
{
    /// <summary>
    /// Adiciona um novo MapModel à base de dados a partir de um MapData.
    /// </summary>
    Task AddFromDataAsync(MapData data, CancellationToken ct = default);
}