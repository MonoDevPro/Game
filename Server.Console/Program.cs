using LiteNetLib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Server.Persistence;
using Simulation.Core.Server.Factories;
using Simulation.Core.Server.Persistence.Contracts;
using Simulation.Core.Server.Staging;
using Simulation.Core.Server.Systems;
using Simulation.Core.Shared;
using Simulation.Core.Shared.Network;
using Simulation.Core.Shared.Options;
using Simulation.Core.Shared.Templates;

Console.Title = "SERVER";

var services = new ServiceCollection();

// 1. Construção da Configuração
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();
        
services
    .ConfigureCustomOptions<ServerOptions>(configuration, ServerOptions.SectionName)
    .ConfigureCustomOptions<GameLoopOptions>(configuration, GameLoopOptions.SectionName)
    .ConfigureCustomOptions<NetworkOptions>(configuration, NetworkOptions.SectionName)
    .ConfigureCustomOptions<SpatialOptions>(configuration, SpatialOptions.SectionName)
    .ConfigureCustomOptions<WorldOptions>(configuration, WorldOptions.SectionName)
    .ConfigureCustomOptions<DebugOptions>(configuration, DebugOptions.SectionName);

services.AddLogging(builder => builder.AddConfiguration(configuration.GetSection("Logging")).AddConsole());
services.AddPersistence(configuration);

services.AddSingleton<EventBasedNetListener>();
services.AddSingleton<ISimulationBuilder, SimulationBuilder>();

using var provider = services.BuildServiceProvider();

var worldOptions = provider.GetRequiredService<WorldOptions>();
var spatialOptions = provider.GetRequiredService<SpatialOptions>();
var networkOptions = provider.GetRequiredService<NetworkOptions>();
var simulationBuilder = provider.GetRequiredService<ISimulationBuilder>();

var stagingPlayer = provider.GetRequiredService<IPlayerStagingArea>();
var stagingMap = provider.GetRequiredService<IMapStagingArea>();
var mapRepo = provider.GetRequiredService<IRepositoryAsync<int, MapData>>();
var playerRepo = provider.GetRequiredService<IRepositoryAsync<int, PlayerData>>();

simulationBuilder
    .WithWorldOptions(worldOptions)
    .WithSpatialOptions(spatialOptions)
    .WithNetworkOptions(networkOptions)
    .WithRootServices(provider);

var systems = simulationBuilder.Build();

systems.Initialize();
Console.WriteLine("Server started with debug packet processing enabled.");

var networkSystem = systems.Get<NetworkSystem>();
var listener = networkSystem.Manager.Listener;
listener.PeerConnectedEvent += async peer => {
    Console.WriteLine($"[Server] Peer connected: {peer.Id}");


    var (exist, data) = await playerRepo.TryGetAsync(peer.Id);
    if (! exist || data is null)
    {
        Console.WriteLine($"[Server] No player data found for peer ID {peer.Id}. Disconnecting.");
        peer.Disconnect();
        return;
    }
    
    stagingPlayer.StageLogin(data);
    Console.WriteLine($"[Server] Player {data.Name} (ID: {data.Id }) logged in.");
};

var statsTimer = 0;
while (true) {
    systems.Update(0.016f);
    
    // Log packet statistics every 10 seconds
    statsTimer++;
    if (statsTimer >= 666) // ~10 seconds at 15ms sleep
    {
        networkSystem.Manager.LogPacketStatistics();
        statsTimer = 0;
    }
    
    Thread.Sleep(15);
}