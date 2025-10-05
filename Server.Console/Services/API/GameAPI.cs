using System.Net;
using System.Net.Http.Json;
using GameWeb.Application.Common.Options;
using GameWeb.Application.Maps.Models;
using Microsoft.Extensions.Logging;

namespace Server.Console.Services.API;

public interface IGameAPI
{
    Task<OptionsDto?> GetOptionsAsync(CancellationToken ct = default);
    Task<(MapDto? data, string etag)> GetMapBinaryWithMetaAsync(int mapId, CancellationToken ct = default);
    Task<string?> GetMapETagAsync(int mapId, CancellationToken ct = default);
}

public class GameAPI : IGameAPI
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GameAPI> _logger;

    public GameAPI(HttpClient httpClient, ILogger<GameAPI> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OptionsDto?> GetOptionsAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Downloading game options...");

            // Usa helper conveniente para JSON
            var optionsData = await _httpClient.GetFromJsonAsync<OptionsDto>("options", ct);
            _logger.LogInformation("Game options downloaded successfully.");
            return optionsData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error downloading game options. Status: {StatusCode}", ex.StatusCode?.ToString() ?? "unknown");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error downloading game options.");
            return null;
        }
    }

    public async Task<(MapDto? data, string etag)> GetMapBinaryWithMetaAsync(int mapId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Downloading map {MapId} as MemoryPack binary...", mapId);

            // Peça o conteúdo e comece a leitura a partir do headers (bom para binários grandes)
            using var response = await _httpClient.GetAsync($"maps/{mapId}", HttpCompletionOption.ResponseHeadersRead, ct);

            // Pega ETag do header (pode vir com aspas)
            var rawEtag = response.Headers.ETag?.Tag ?? string.Empty;
            var etag = NormalizeEtag(rawEtag);

            // Se o servidor retornar 304 Not Modified, não precisamos baixar o corpo
            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                _logger.LogInformation("Map {MapId} not modified. ETag: {ETag}", mapId, etag);
                return (null, etag);
            }

            // Garante sucesso para status diferentes de 304
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);

            if (bytes.Length == 0)
            {
                _logger.LogWarning("Binary data for map {MapId} is empty.", mapId);
                return (null, string.Empty);
            }

            var mapData = MemoryPack.MemoryPackSerializer.Deserialize<MapDto>(bytes);

            _logger.LogInformation(
                "Map '{MapName}' (ID: {MapId}) downloaded. Size: {Size} bytes, ETag: {ETag}",
                mapData?.Name, mapId, bytes.Length, etag);

            return (mapData, etag);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error downloading map {MapId}. Status: {StatusCode}", mapId, ex.StatusCode?.ToString() ?? "unknown");
            return (null, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error downloading map {MapId}.", mapId);
            return (null, string.Empty);
        }
    }

    public async Task<string?> GetMapETagAsync(int mapId, CancellationToken ct = default)
    {
        try
        {
            // HEAD é mais leve — só headers
            using var request = new HttpRequestMessage(HttpMethod.Head, $"maps/{mapId}/meta");
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            // Se o endpoint HEAD não estiver implementado, o servidor pode responder 405 ou devolver um GET
            if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                _logger.LogDebug("HEAD not allowed for meta endpoint; falling back to GET for map {MapId}.", mapId);
                using var fallback = await _httpClient.GetAsync($"maps/{mapId}/meta", ct);
                fallback.EnsureSuccessStatusCode();
                var metaFallback = await fallback.Content.ReadFromJsonAsync<MapMetaDto>(ct);
                return NormalizeEtag(metaFallback?.ETag);
            }

            response.EnsureSuccessStatusCode();

            var rawEtag = response.Headers.ETag?.Tag;

            if (!string.IsNullOrEmpty(rawEtag))
                return NormalizeEtag(rawEtag);

            // Se o HEAD não retornar ETag, tente ler o corpo JSON caso o servidor faça isso:
            try
            {
                var meta = await response.Content.ReadFromJsonAsync<MapMetaDto>(ct);
                return NormalizeEtag(meta?.ETag);
            }
            catch
            {
                // sem meta disponível
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get ETag for map {MapId}. Status: {StatusCode}", mapId, ex.StatusCode?.ToString() ?? "unknown");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get ETag for map {MapId}.", mapId);
            return null;
        }
    }

    private static string NormalizeEtag(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        // Remove aspas wrapping (ex: "W/\"123\"")
        return raw.Trim().Trim('\"');
    }
}