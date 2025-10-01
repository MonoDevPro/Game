using System.Net.Http.Json;
using Ardalis.GuardClauses;
using GameWeb.Application.Maps.Models;
using Microsoft.Extensions.Logging;

namespace Server.Console.Services;

public class MapApiClient : IMapApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MapApiClient> _logger;

    public MapApiClient(HttpClient httpClient, ILogger<MapApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<MapDto?> GetMapByIdAsync(int mapId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Buscando mapa com ID {MapId} na API...", mapId);
            
            // Usa o método de extensão GetFromJsonAsync para fazer o GET e deserializar o JSON
            var map = await _httpClient.GetFromJsonAsync<MapDto>($"maps/{mapId}", cancellationToken);

            Guard.Against.Null(map, message: $"Mapa com ID {mapId} retornou nulo da API.");

            _logger.LogInformation("Mapa '{MapName}' (ID: {MapId}) recebido com sucesso.", map.Name, map.Id);
            return map;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao tentar buscar o mapa com ID {MapId}. Status: {StatusCode}", mapId, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Um erro inesperado ocorreu ao buscar o mapa com ID {MapId}.", mapId);
            return null;
        }
    }
    
    public async Task<byte[]?> GetMapBinaryByIdAsync(int mapId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Baixando dados binários do mapa com ID {MapId}...", mapId);

            // Faz a requisição e lê a resposta diretamente como um array de bytes
            var mapBytes = await _httpClient.GetByteArrayAsync($"client/map/{mapId}", cancellationToken);

            if (mapBytes.Length == 0)
            {
                _logger.LogWarning("Os dados binários para o mapa ID {MapId} estão vazios.", mapId);
                return null;
            }

            _logger.LogInformation("Recebidos {ByteCount} bytes para o mapa ID {MapId}.", mapBytes.Length, mapId);
            return mapBytes;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao tentar baixar os dados binários do mapa {MapId}. Status: {StatusCode}", mapId, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Um erro inesperado ocorreu ao baixar os dados binários do mapa {MapId}.", mapId);
            return null;
        }
    }
}