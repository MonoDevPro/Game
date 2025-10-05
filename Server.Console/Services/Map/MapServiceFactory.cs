using GameWeb.Application.Maps.Models;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Utils;

namespace Server.Console.Services.Map;

public sealed class MapServiceFactory(ILogger<MapServiceFactory> logger)
{
    private readonly string _cacheDir = Path.Combine(AppContext.BaseDirectory, "cache", "maps");

    /// <summary>Cria MapService a partir de MapDto.</summary>
    public MapService CreateFromDto(MapDto dto)
    {
        // mantendo sua factory existente
        return MapService.CreateFromTemplate(dto);
    }

    /// <summary>Tenta carregar do disco: deserializa bytes e meta (ETag).</summary>
    public async Task<(MapService map, string etag)?> TryLoadFromDiskCacheAsync(int mapId, CancellationToken ct = default)
    {
        var dataPath = Path.Combine(_cacheDir, $"map-{mapId}.mpack");
        var metaPath = Path.Combine(_cacheDir, $"map-{mapId}.meta");

        if (!File.Exists(dataPath) || !File.Exists(metaPath))
            return null;

        try
        {
            var bytes = await File.ReadAllBytesAsync(dataPath, ct);
            var meta = await File.ReadAllTextAsync(metaPath, ct);
            var etag = meta?.Trim() ?? string.Empty;

            var dto = MemoryPack.MemoryPackSerializer.Deserialize<MapDto>(bytes);
            if (dto == null) return null;

            var mapService = CreateFromDto(dto);
            return (mapService, etag);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load map {MapId} from disk cache.", mapId);
            return null;
        }
    }

    /// <summary>Salva bytes + etag no disco (cria diretório se necessário).</summary>
    public async Task SaveToDiskCacheAsync(int mapId, MapDto mapData, string etag, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(_cacheDir);

            var dataPath = Path.Combine(_cacheDir, $"map-{mapId}.mpack");
            var metaPath = Path.Combine(_cacheDir, $"map-{mapId}.meta");

            var bytes = MemoryPack.MemoryPackSerializer.Serialize(mapData);

            await File.WriteAllBytesAsync(dataPath, bytes, ct);
            await File.WriteAllTextAsync(metaPath, etag ?? string.Empty, ct);

            logger.LogDebug("Map {MapId} saved to disk cache.", mapId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save map {MapId} to disk cache.", mapId);
        }
    }
}