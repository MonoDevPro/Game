using Microsoft.Extensions.Logging;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Client;
using Simulation.Core.Options;
using Simulation.Abstractions.Network;
using Simulation.Core.ECS.Shared;
using Arch.Core.Extensions;
using Microsoft.Extensions.Options;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// --- Configuração do Host ---
Console.Title = "CLIENT";

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<ISimulationBuilder<float>, ClientSimulationBuilder>();
        services.Configure<WorldOptions>(context.Configuration.GetSection(WorldOptions.SectionName));
        services.Configure<NetworkOptions>(context.Configuration.GetSection(NetworkOptions.SectionName));
    })
    .Build();

// --- Inicialização da Simulação ---
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Cliente a iniciar...");

var builder = host.Services.GetRequiredService<ISimulationBuilder<float>>();
var worldOptions = host.Services.GetRequiredService<IOptions<WorldOptions>>().Value;
var networkOptions = host.Services.GetRequiredService<IOptions<NetworkOptions>>().Value;

var build = builder
    .WithWorldOptions(worldOptions)
    .WithNetworkOptions(networkOptions)
    .WithRootServices(host.Services)
    
    // --- Regista os mesmos componentes que o servidor ---
    // Isto é crucial para que o cliente saiba como desserializar os pacotes recebidos
    // e para enviar os seus próprios componentes de "intenção".
    .WithSynchronizedComponent<Position>(new SyncOptions { Authority = Authority.Server })
    .WithSynchronizedComponent<Health>(new SyncOptions { Authority = Authority.Server })
    .WithSynchronizedComponent<StateComponent>(new SyncOptions { Authority = Authority.Server })
    .WithSynchronizedComponent<Direction>(new SyncOptions { Authority = Authority.Server })
    .WithSynchronizedComponent<InputComponent>(new SyncOptions { Authority = Authority.Client })
    
    .Build();

logger.LogInformation("Pipeline de simulação do cliente construída. A entrar no loop principal.");

// --- Loop Principal do Jogo (Simulado) ---
Entity? localPlayer = null;
var world = build.World;
var pipeline = build.Group;

while (true)
{
    // Num jogo real, a entidade do jogador seria criada pelo servidor após a conexão.
    // Este loop procura pela entidade que representa o nosso jogador.
    if (localPlayer == null || !localPlayer.Value.IsAlive())
    {
        // Esta query encontra a primeira entidade com PlayerId (assumindo que é a nossa)
        var playerQuery = new QueryDescription().WithAll<PlayerId>();
        world.Query(in playerQuery, (Entity entity) => {
            localPlayer = entity;
            logger.LogInformation("Entidade do jogador local encontrada: {entity}", entity);
        });
    }

    // Simula a entrada do jogador e adiciona um InputComponent para ser enviado
    if (localPlayer.HasValue)
    {
        // Aqui iria a lógica real para ler o teclado.
        // Como exemplo, vamos apenas adicionar um componente de input para mover para a direita.
        // O GenericSyncSystem<InputComponent> irá detetar e enviar este componente para o servidor.
        world.Add(localPlayer.Value, new InputComponent(IntentFlags.Move, InputFlags.Right));
    }

    // Atualiza a pipeline: processa eventos de rede recebidos, atualiza sistemas, etc.
    pipeline.Update(0.016f);
    
    // Controla a taxa de atualização para aproximadamente 66 FPS.
    Thread.Sleep(15);
}
