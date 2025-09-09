using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Simulation.Core.Server.Persistence.Contracts;
using Simulation.Core.Shared.Components;
using Simulation.Core.Shared.Templates;

namespace Simulation.Core.Shared.Utils.Map;

/// <summary>
/// Serviço singleton responsável por carregar, descarregar e fornecer
/// acesso a todas as instâncias de mapas (MapService) ativas no servidor.
/// </summary>
public class MapManagerService(IRepositoryAsync<int, MapData> mapTemplateRepository, ILogger<MapManagerService> logger)
{
    private readonly ConcurrentDictionary<int, MapService> _loadedMaps = new();

    /// <summary>
    /// Carrega um mapa do repositório para a memória, se ainda não estiver carregado.
    /// </summary>
    public async Task LoadMapAsync(int mapId)
    {
        if (_loadedMaps.ContainsKey(mapId))
        {
            return; // Mapa já está carregado
        }

        var template = await mapTemplateRepository.GetAsync(mapId);
        if (template != null)
        {
            var mapService = MapService.CreateFromTemplate(template);
            _loadedMaps.TryAdd(mapId, mapService);
        }
        else
        {
            // Logar erro: template do mapa não encontrado
            logger.LogError("LoadMapAsync: Map template with ID {MapId} not found in repository.", mapId);
        }
    }
    
    /// <summary>
    /// Obtém a instância de um mapa carregado.
    /// </summary>
    public MapService? GetMap(int mapId)
    {
        _loadedMaps.TryGetValue(mapId, out var map);
        return map;
    }
    
    /// <summary>
    /// Verifica se uma determinada posição em um mapa específico está bloqueada.
    /// Esta é a principal função a ser usada por sistemas de jogo.
    /// </summary>
    public bool IsTileBlocked(int mapId, Position pos)
    {
        if (_loadedMaps.TryGetValue(mapId, out var map))
        {
            if (map.InBounds(pos))
            {
                return map.IsBlocked(pos);
            }
        }
        // Se o mapa não existe ou a posição está fora dos limites, considera bloqueado por segurança.
        return true; 
    }
}