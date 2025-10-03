using System.Net.Http.Headers;
using GameWeb.Application.Common.Options;using GameWeb.Application.Maps.Models;
using Microsoft.Extensions.Configuration;
using Server.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Simulation.Core;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Utils;
using Simulation.Core.Ports.ECS;
using Simulation.Core.Server.API;
using Simulation.Core.Server.ECS;
using Simulation.Core.Server.Map;
using Simulation.Network;

// ---------------------------
// 1) Bootstrap temporário: cria um ServiceProvider mínimo para buscar Options do API.
// ---------------------------

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger? logger = null)
{
    // Exponential backoff com jitter para evitar bursts
    var jitterer = new Random();
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, // 3 tentativas
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(jitterer.Next(0, 200)),
            onRetry: (outcome, timespan, retryCount, ctx) =>
            {
                logger?.LogWarning("Retry {RetryCount} after {Delay} due to {Reason}", retryCount, timespan, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
            });
}

// Cria um provider mínimo para chamar a API e obter OptionsDto.
// Usamos um provider separado E descartável para não misturar estados com o host principal.
static IServiceProvider BuildBootstrapProvider()
{
    var sc = new ServiceCollection();
    sc.AddLogging(cfg => cfg.AddConsole());
    sc.AddMemoryCache(options => { options.SizeLimit = 1024; });
    sc.AddSingleton<MapRepository>(); // repositório de mapas
    sc.AddSingleton<MapServiceFactory>(); // fábrica de MapService
    sc.AddHttpClient<IGameAPI, GameAPI>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5000/api/"); // fallback dev
            client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("GameServer/1.0"));
        })
        .AddPolicyHandler((sp, req) => GetRetryPolicy(sp.GetService<ILoggerFactory>()?.CreateLogger("Bootstrap") ?? null));

    return sc.BuildServiceProvider();
}

static async Task<OptionsDto> FetchOptionsDtoAsync(IServiceProvider bootstrap)
{
    var logger = bootstrap.GetRequiredService<ILoggerFactory>().CreateLogger("Bootstrap");
    var api = bootstrap.GetRequiredService<IGameAPI>();

    logger.LogInformation("Fetching OptionsDto from Game API...");
    var options = await api.GetOptionsAsync();
    if (options == null)
    {
        logger.LogCritical("Failed to load game options from API (null).");
        throw new Exception("Failed to load game options from API.");
    }

    logger.LogInformation("OptionsDto fetched. WorldOptions: {WorldOptions}, NetworkOptions: {NetworkOptions}",
        options.World.ToString(),
        options.Network.ToString());
    return options;
}

static async Task<MapService?> LoadMapServiceAsync(int mapId, IServiceProvider provider, CancellationToken ct = default)
{
    // Antes de iniciar o host (StartAsync dos hosted services), fazemos o preload de mapas
    using (var scope = provider.CreateScope())
    {
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Bootstrap.Preload");
        var mapRepo = sp.GetRequiredService<MapRepository>();
    
        logger.LogInformation("Preloading map {MapId}...", mapId);
        var mapService = await mapRepo.GetMapService(mapId); // força preload do mapa na memória
    
        if (mapService == null)
        {
            logger.LogCritical("Failed to preload map {MapId}. Cannot start server.", mapId);
            return mapService;
        }

        logger.LogInformation("Map {MapId} preloaded successfully.", mapId);
        return mapService;
    }
}

// ---------------------------
// 2) Main: construir host com as options já resolvidas e inicializar recursos (preload).
// ---------------------------

var bootstrapProvider = BuildBootstrapProvider();
OptionsDto optionsDto;
MapService mapService;
using (bootstrapProvider as IDisposable) // garante disposal ao terminar
{
    optionsDto = await FetchOptionsDtoAsync(bootstrapProvider);
    var mapInstanceInfo = new MapInstanceInfo { MapId = 1 };
    
    mapService = await LoadMapServiceAsync(mapInstanceInfo.MapId, bootstrapProvider) ?? throw new Exception("Failed to load map service.");
}

// Agora constrói o host principal com as opções resolvidas
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        // logging
        services.AddLogging(cfg => cfg.AddConsole());

        // Registra HttpClient para IGameAPI no host principal também
        services.AddHttpClient<IGameAPI, GameAPI>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:5000/api/");
                client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("GameServer/1.0"));
            })
            .AddPolicyHandler((sp, req) => GetRetryPolicy(new Logger<GameAPI>(sp.GetRequiredService<ILoggerFactory>())));
        
        services.AddSingleton<IOptions<WorldOptions>>(sp => Options.Create(optionsDto.World ?? new WorldOptions()));
        services.AddSingleton<IOptions<NetworkOptions>>(sp => Options.Create(optionsDto.Network ?? new NetworkOptions()));
        services.AddSingleton<IOptions<AuthorityOptions>>(sp => Options.Create(new AuthorityOptions { Authority = Authority.Server }));

        // Outros serviços do app
        services.AddSingleton(TimeProvider.System);
        
        services.AddSingleton<IWorldSaver, WorldSaver>();
        services.AddSingleton<MapService>(mapService);

        services.AddNetworking();
        services.AddSingleton<ISimulationBuilder<float>, ServerSimulationBuilder>();

        // Hosted service (GameServerHost) - ele deverá resolver MapService via MapRepository no StartAsync
        services.AddHostedService<GameServerHost>();
    })
    .Build();

// Finalmente inicia o host (start dos hosted services)
await host.RunAsync();