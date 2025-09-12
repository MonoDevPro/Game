using LiteNetLib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Server.Persistence;
using Simulation.Core.ECS.Server;
using Simulation.Core.ECS.Server.Staging;
using Simulation.Core.ECS.Server.Systems;
using Simulation.Core.Models;
using Simulation.Core.Options;
using Simulation.Core.Persistence.Contracts;

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

// Maps loading
var mapStaging = provider.GetRequiredService<IMapStagingArea>();
var mapRepo = provider.GetRequiredService<IRepositoryAsync<int, MapModel>>();
var maps = await mapRepo.GetAllAsync();
foreach (var map in maps)
{
    mapStaging.StageMapLoaded(map);
    Console.WriteLine($"[Server] Map loaded: {map.Name} (ID: {map.MapId})");
}

// Players tests Staging
var stagingPlayer = provider.GetRequiredService<IPlayerStagingArea>();
var playerRepo = provider.GetRequiredService<IRepositoryAsync<int, PlayerModel>>();
var players = await playerRepo.GetAllAsync();
foreach (var player in players)
{
    stagingPlayer.StageLogin(player);
    Console.WriteLine($"[Server] Player loaded: {player.Name} (ID: {player.Id})");
}

// Build and start the simulation systems
var systems = provider.GetRequiredService<ISimulationBuilder>()
    .WithWorldOptions(provider.GetRequiredService<WorldOptions>())
    .WithSpatialOptions(provider.GetRequiredService<SpatialOptions>())
    .WithRootServices(provider)
    .Build();

systems.Initialize();

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