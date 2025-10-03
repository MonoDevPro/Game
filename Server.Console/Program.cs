using GameWeb.Application.Common.Options;
using Server.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Server.Console.Services;
using Simulation.Core;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Utils;
using Simulation.Core.Ports.ECS;
using Simulation.Core.Server.ECS;
using Simulation.Network;

var apiServiceCollection = new ServiceCollection();
apiServiceCollection.AddLogging(configure => configure.AddConsole());
apiServiceCollection.AddSingleton<MapRepository>();
apiServiceCollection.AddHttpClient<IGameAPI, GameAPI>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5000/api/"); // Fallback para dev
}).AddPolicyHandler(GetRetryPolicy()); // Adiciona a política de resiliência

var apiServiceProvider = apiServiceCollection.BuildServiceProvider();

var options = await GetOptionsAsync(apiServiceProvider);
await PreloadMapsAsync(apiServiceProvider, options.Map);
var mapService = await GetMapServiceAsync(apiServiceProvider, 1);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddLogging(configure => configure.AddConsole());
        
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<MapOptions>(options.Map);
        services.AddSingleton<WorldOptions>(options.World);
        services.AddSingleton<NetworkOptions>(options.Network);
        services.AddSingleton<AuthorityOptions>(new AuthorityOptions { Authority = Authority.Server });
        
        services.AddSingleton<IOptions<MapOptions>>(sp => Options.Create(sp.GetRequiredService<MapOptions>()));
        services.AddSingleton<IOptions<WorldOptions>>(sp => Options.Create(sp.GetRequiredService<WorldOptions>()));
        services.AddSingleton<IOptions<NetworkOptions>>(sp => Options.Create(sp.GetRequiredService<NetworkOptions>()));
        services.AddSingleton<IOptions<AuthorityOptions>>(sp => Options.Create(sp.GetRequiredService<AuthorityOptions>()));
        
        services.AddSingleton<IWorldSaver, WorldSaver>();
        
        services.AddNetworking();
        
        services.AddSingleton<ISimulationBuilder<float>, ServerSimulationBuilder>();
        
        services.AddSingleton(mapService);
        
        services.AddHostedService<GameServerHost>();
        
    })
    .Build();

await host.RunAsync();

// Define uma política de retentativa simples: tentar 3 vezes com um pequeno atraso
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // Lida com erros de rede, 5xx e 408
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static async Task<OptionsDto> GetOptionsAsync(IServiceProvider provider)
{
    var api = provider.GetRequiredService<IGameAPI>();
    var options = await api.GetOptionsAsync();
    if (options == null)
        throw new Exception($"Failed to load game options from API.");
    return options;
}

static async Task PreloadMapsAsync(IServiceProvider services, MapOptions options)
{
    if (options.Maps.Length == 0)
        throw new Exception("No maps configured in game options.");
    
    var mapRepo = services.GetRequiredService<MapRepository>();
    foreach (var mapInfo in options.Maps)
    {
        var mapService = await mapRepo.GetMapService(mapInfo.Id);
        if (mapService == null)
            throw new Exception($"Failed to load map with ID {mapInfo}");
    }
}

static async Task<MapService> GetMapServiceAsync(IServiceProvider services, int mapId)
{
    var mapRepo = services.GetRequiredService<MapRepository>();
    var mapService = await mapRepo.GetMapService(mapId);
    
    if (mapService == null)
        throw new Exception($"Failed to load map with ID {mapId}");
    
    return mapService;
}