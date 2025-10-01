using GameWeb.Application.Maps.Models;

namespace Server.Console.Services;

public interface IMapApiClient
{
    Task<MapDto?> GetMapByIdAsync(int mapId, CancellationToken cancellationToken = default);
    
    Task<byte[]?> GetMapBinaryByIdAsync(int mapId, CancellationToken cancellationToken = default);
}