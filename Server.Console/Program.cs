using GameWeb.Application.Common.Options;
using GameWeb.Application.Maps.Models;
using GameWeb.Application.Players.Models;
using MemoryPack;
using Server.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Server.Console.Services;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Utils;
using Simulation.Core.Ports.ECS;
using Simulation.Core.Server.ECS;
using Simulation.Network;

var ApiServiceCollection = new ServiceCollection();

// Registra o IHttpClientFactory e configura um cliente HTTP nomeado "ApiClient"
ApiServiceCollection.AddHttpClient<IMapApiClient, MapApiClient>(client =>
    {
        // Obtém a URL base da API do appsettings.json
        client.BaseAddress = new Uri("http://localhost:5000/api/"); // Fallback para dev
    })
    .AddPolicyHandler(GetRetryPolicy()); // Adiciona a política de resiliência
var apiServiceProvider = ApiServiceCollection.BuildServiceProvider();

var mapApiClient = apiServiceProvider.GetRequiredService<IMapApiClient>();

// Exemplo de uso do cliente HTTP para buscar um mapa
var mapIdToLoad = 1; // ID do mapa que deseja carregar
byte[]? mapData = await mapApiClient.GetMapBinaryByIdAsync(mapIdToLoad);
MapDto? mapDto;

if (mapData != null)
{
    var map = MemoryPackSerializer.Deserialize<MapDto>(mapData);
    if (map == null)
    {
        Console.WriteLine("Falha ao desserializar os dados do mapa.");
        return;
    }
    
    mapDto = map;
    Console.WriteLine($"Mapa '{map.Name}' (ID: {map.Id}) carregado com sucesso.");
}
else
{
    Console.WriteLine("Não foi possível carregar o mapa.");
    // Dependendo do design, talvez queira parar o servidor aqui.
    return;
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<WorldOptions>(context.Configuration.GetSection(WorldOptions.SectionName));
        services.Configure<NetworkOptions>(context.Configuration.GetSection(NetworkOptions.SectionName));
        services.Configure<AuthorityOptions>(context.Configuration.GetSection(AuthorityOptions.SectionName));
        
        services.AddLogging(configure => configure.AddConsole());
        
        services.AddSingleton(TimeProvider.System);
        
        services.AddSingleton<IWorldSaver, WorldSaver>();
        
        services.AddNetworking();
        
        services.AddSingleton<ISimulationBuilder<float>, ServerSimulationBuilder>();
        
        services.AddSingleton(MapService.CreateFromTemplate(mapDto));
        
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

namespace Server.Console
{
    public class WorldSaver : IWorldSaver
    {
        public void StageSave(PlayerDto dto)
        {
            // Implementar a lógica de salvamento do mundo aqui
            System.Console.WriteLine($"Salvando dados do jogador: {dto.Id}");
        }
    }
    
    
}