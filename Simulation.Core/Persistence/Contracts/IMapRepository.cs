using Simulation.Core.ECS.Data;
using Simulation.Core.Persistence.Models;

namespace Simulation.Core.Persistence.Contracts;

/// <summary>
/// Contrato para o repositório de mapas, estendendo o repositório genérico
/// com operações específicas para MapData.
/// </summary>
public interface IMapRepository
{
    /// <summary>
    /// Adiciona um novo MapModel à base de dados a partir de um MapData.
    /// </summary>
    Task AddFromDataAsync(MapData data, CancellationToken ct = default);

    Task<MapData?> GetMapAsync(int id, CancellationToken ct = default);
}