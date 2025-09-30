using Application.Abstractions;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Maps.Services;

public interface IMapRepository
{
    Task<Map?> GetMapEntityAsync(int id, CancellationToken ct = default);
    Task<MapData?> GetMapAsync(int id, CancellationToken ct = default);
    Task SaveMapAsync(Map map, CancellationToken ct = default);
    Task DeleteMapAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<MapData>> GetAllMapsAsync(CancellationToken ct = default);
}
