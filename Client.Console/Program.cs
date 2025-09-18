using Arch.Core;
using Client.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.Auth.Messages;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Builders;
using Simulation.Core.ECS.Indexes.Map;
using Simulation.Core.ECS.Staging;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;
using Simulation.Network;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client.Consoles;

// --- Configuração do Host ---
Console.Title = "CLIENT";

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<WorldOptions>(context.Configuration.GetSection(WorldOptions.SectionName));
        services.Configure<NetworkOptions>(context.Configuration.GetSection(NetworkOptions.SectionName));
        
        services.AddNetworking();
        
        services.AddSingleton<ISimulationBuilder<float>, ClientSimulationBuilder>();

        services.AddSingleton<IWorldStaging, WorldStagingClient>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var processor = host.Services.GetRequiredService<IChannelProcessorFactory>().CreateOrGet(NetworkChannel.Authentication);

// --- Inicialização da Simulação (necessária para a rede funcionar) ---
var builder = host.Services.GetRequiredService<ISimulationBuilder<float>>();
var worldOptions = host.Services.GetRequiredService<IOptions<WorldOptions>>().Value;
var mapDatas = DataSeeder.GetMapSeeds();
var mapService = MapService.CreateFromTemplate(mapDatas.First(a => a.Id == 1));

var simulation = builder
    .WithWorldOptions(worldOptions)
    .WithMapService(mapService)
    .WithRootServices(host.Services)
    .Build();

logger.LogInformation("Pipeline de simulação construída. A iniciar o loop de atualização em segundo plano.");

// --- Loop de atualização em segundo plano ---
var cts = new CancellationTokenSource();
var updateTask = Task.Run(() =>
{
    while (!cts.IsCancellationRequested)
    {
        simulation.Systems.Update(0.016f); // Aproximadamente 60 FPS
        Thread.Sleep(16);
    }
}, cts.Token);


// --- Fluxo de Autenticação ---
var authCompleted = new TaskCompletionSource<LoginResponse>();
var registrationCompleted = new TaskCompletionSource<RegisterResponse>();

processor.RegisterHandler<LoginResponse>((peer, packet) =>
{
    if (packet.Success)
    {
        logger.LogInformation("Login bem-sucedido! AccountId: {AccountId}, Token: {Token}", packet.AccountId, packet.SessionToken);
        authCompleted.TrySetResult(packet);
    }
    else
    {
        logger.LogWarning("Falha no login: {Message}", packet.Message);
        authCompleted.TrySetException(new Exception(packet.Message));
    }
});

processor.RegisterHandler<RegisterResponse>((peer, packet) =>
{
    if (packet.Success)
    {
        logger.LogInformation("Registro bem-sucedido: {Message}", packet.Message);
        registrationCompleted.TrySetResult(packet);
    }
    else
    {
        logger.LogWarning("Falha no registro: {Message}", packet.Message);
        registrationCompleted.TrySetException(new Exception(packet.Message));
    }
});

var loginResponse = await HandleAuthenticationAsync(processor);

if (loginResponse != null)
{
    logger.LogInformation("Autenticação concluída. O loop principal do jogo assume agora.");
    // A tarefa de atualização em segundo plano (updateTask) pode continuar a correr,
    // pois o ClientGameLoop agora só precisa de se focar na lógica específica do jogo (ex: input do jogador).
    // Se ClientGameLoop tivesse seu próprio loop de update, poderíamos cancelar este com cts.Cancel().
    new ClientGameLoop(simulation.World, simulation.Systems, processor).Run();
}
else
{
    logger.LogWarning("Não foi possível autenticar. A encerrar o cliente.");
}

// --- Encerramento ---
cts.Cancel(); // Garante que a tarefa de fundo pare
await updateTask; // Espera a tarefa terminar
await host.StopAsync();


// --- Métodos de fluxo de autenticação ---
async Task<LoginResponse?> HandleAuthenticationAsync(IChannelEndpoint endpoint)
{
    while (true)
    {
        Console.WriteLine("\n--- Menu de Autenticação ---");
        Console.WriteLine("1. Login");
        Console.WriteLine("2. Registrar nova conta");
        Console.WriteLine("3. Sair");
        Console.Write("Escolha uma opção: ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                try
                {
                    var (username, password) = GetCredentials();
                    endpoint.SendToServer(new LoginRequest(username, password), NetworkDeliveryMethod.ReliableOrdered);
                    var response = await authCompleted.Task;
                    return response;
                }
                catch (Exception ex)
                {
                    logger.LogError("Erro durante o login: {ErrorMessage}", ex.Message);
                    authCompleted = new TaskCompletionSource<LoginResponse>(); // Reset para a próxima tentativa
                }
                break;

            case "2":
                try
                {
                    var (username, password) = GetCredentials();
                    endpoint.SendToServer(new RegisterRequest(username, password), NetworkDeliveryMethod.ReliableOrdered);
                    await registrationCompleted.Task;
                    Console.WriteLine("Conta registrada com sucesso! Por favor, faça o login.");
                }
                catch (Exception ex)
                {
                    logger.LogError("Erro durante o registro: {ErrorMessage}", ex.Message);
                }
                finally
                {
                    registrationCompleted = new TaskCompletionSource<RegisterResponse>();
                }
                break;
                
            case "3":
                return null;

            default:
                Console.WriteLine("Opção inválida. Tente novamente.");
                break;
        }
    }
}

(string username, string password) GetCredentials()
{
    Console.Write("Username: ");
    var username = Console.ReadLine() ?? string.Empty;
    Console.Write("Password: ");
    var password = Console.ReadLine() ?? string.Empty;
    return (username, password);
}


namespace Client.Consoles
{
    public class ClientGameLoop(World world, GroupSystems group, IChannelEndpoint endpoint)
    {
        private readonly World _world = world;
        private readonly GroupSystems _group = group;
        private Entity? _localPlayer;

        public void Run()
        {
            // Como o loop de atualização já está a correr em segundo plano,
            // esta função pode ser usada para lógica de jogo específica que precisa de um loop,
            // como ler o input do console de forma contínua.
            // Por agora, vamos apenas impedir que a aplicação termine.
            System.Console.WriteLine("Loop de jogo iniciado. Pressione Ctrl+C para sair.");
            while (true)
            {
                // TODO: Adicionar lógica de input do jogador ou outras tarefas do loop principal.
                Thread.Sleep(100);
            }
        }
    }
}