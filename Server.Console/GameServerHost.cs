using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Authentication;
using Server.Persistence.Seeds;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Indexes.Map;
using Simulation.Core.ECS.Systems;
using Simulation.Core.Options;
using Simulation.Core.Persistence.Contracts;
using Simulation.Core.Persistence.Contracts.Repositories;
using Simulation.Core.Persistence.Models;

namespace Server.Console;

public class GameServerHost(IServiceProvider serviceProvider, ILogger<GameServerHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Game Server Host está a iniciar.");
        
        // Cria scope de forma assíncrona (await using) para permitir DisposeAsync nos serviços.
        await using var scope = serviceProvider.CreateAsyncScope();
        
        // ECS Builder e Opções do Mundo
        var builder = scope.ServiceProvider.GetRequiredService<ISimulationBuilder<float>>();
        var worldOptions = scope.ServiceProvider.GetRequiredService<IOptions<WorldOptions>>().Value;
        // Carregar o mapa (ID 1 por agora)
        var mapService = await LoadMapServiceAsync(serviceProvider, mapId: 1);
        // Construir o mundo e os sistemas
        var (groupSystems, world, worldManager) = builder
            .WithMapService(mapService)
            .WithWorldOptions(worldOptions)
            .WithRootServices(scope.ServiceProvider)
            .Build();
        var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
        var playerData = await repository.GetPlayerByName("Filipe", stoppingToken);
        if (playerData == null)
        {
            logger.LogError("Jogador 'Filipe' não encontrado na base de dados. Encerrando o servidor.");
            return;
        }
        var data = playerData.Value with{ MoveSpeed = 1.5f, PosX = 5, PosY = 5};
        EntityFactorySystem.CreatePlayerEntity(world, data);
        
        // AuthService
        var authService = scope.ServiceProvider.GetRequiredService<AuthService>();

        while (!stoppingToken.IsCancellationRequested)
        {
            authService.AuthUpdate(0.016f);
            
            groupSystems.Update(0.016f);
            
            await Task.Delay(15, stoppingToken);
        }
    }

    static async Task<MapService> LoadMapServiceAsync(IServiceProvider services, int mapId)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var mapRepository = scope.ServiceProvider.GetRequiredService<IMapRepository>();

        logger.LogInformation("A carregar o mapa com ID {MapId}...", mapId);

        var mapData = await mapRepository.GetMapAsync(mapId);
        if (mapData == null)
            throw new InvalidOperationException("MapData não encontrado no repositório.");

        var mapService = MapService.CreateFromTemplate(mapData.Value);

        // --- Preparar dados para log (sem usar MapData.ToString) ---
        int tilesLen = mapData.Value.TilesRowMajor?.Length ?? 0;
        int collLen = mapData.Value.CollisionRowMajor?.Length ?? 0;

        // Preview dos primeiros N tiles (do row-major original, mais legível)
        const int previewCount = 8;
        string tilesPreview;
        if (tilesLen == 0)
        {
            tilesPreview = "(none)";
        }
        else
        {
            var tiles = mapData.Value.TilesRowMajor ?? Array.Empty<TileType>();
            var previewSeq = tiles
                .Take(Math.Min(previewCount, tiles.Length))
                .Select(t => t.ToString());
        
            tilesPreview = string.Join(", ", previewSeq);
            if (tilesLen > previewCount) tilesPreview += ", ...";
        }

        // Conta todas as células bloqueadas usando o método que você adicionou.
        // Se o mapa for muito grande e você preferir amostragem, passe parallel:true ou
        // use CountBlockedCellsSample (se implementar).
        long blockedCells = mapService.CountBlockedCells(parallel: false);

        // Log estruturado com todos os campos importantes
        logger.LogInformation(
            "Mapa carregado: Name={MapName}, Id={MapId}, Size={Width}x{Height}, UsePadded={UsePadded}, BorderBlocked={BorderBlocked}, TilesLength={TilesLength}, CollisionLength={CollisionLength}, TilesPreview=[{TilesPreview}], BlockedCells={BlockedCells}",
            mapService.Name,
            mapService.Id,
            mapService.Width,
            mapService.Height,
            mapService.UsePadded,
            mapService.BorderBlocked,
            tilesLen,
            collLen,
            tilesPreview,
            blockedCells
        );

        return mapService;
    }
}