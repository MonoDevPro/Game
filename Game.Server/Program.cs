using Game.Abstractions;
using Game.Abstractions.Network;
using Game.Core;
using Game.Core.Security;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Network;
using Game.Persistence;
using Game.Server;
using Game.Server.Authentication;
using Game.Server.Loop;
using Game.Server.Players;
using Game.Server.Sessions;

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Database
var connectionString = configuration.GetConnectionString("GameDatabase") ?? "Data Source=identifier.sqlite";
services.AddGameDatabase(connectionString);

// Core services
services.AddSingleton(CreateDefaultMap);
services.AddSingleton<GameSimulation>();
services.AddSingleton<PlayerSessionManager>();
services.AddSingleton<PlayerSpawnService>();
services.AddSingleton<PlayerStateBroadcaster>();
services.AddSingleton<GameServer>();

services.AddSingleton<IPasswordHasher, PasswordHasher>();
services.AddScoped<AccountLoginService>();
services.AddScoped<PlayerPersistenceService>();

// Networking
services.AddNetworking(new NetworkOptions
{
    IsServer = true,
    ServerAddress = "0.0.0.0",
    ServerPort = 7777,
    ConnectionKey = "default",
    PingIntervalMs = 2000,
    DisconnectTimeoutMs = 5000,
    MaxMessagesPerSecond = 120,
    MaxMessageSizeBytes = 1024 * 64
});

// Hosted services
services.AddHostedService<BootstrapHostedService>();
services.AddHostedService<GameLoopService>();
services.AddHostedService<NetworkLoopService>();

var host = builder.Build();
await host.RunAsync();

MapService CreateDefaultMap(IServiceProvider _)
{
    const int width = 64;
    const int height = 64;
    var tiles = new TileType[width * height];
    for (var i = 0; i < tiles.Length; i++)
    {
        tiles[i] = TileType.Floor;
    }

    var collision = new byte[width * height];

    var mapTemplate = new Map
    {
        Id = 1,
        Name = "Training Grounds",
        Width = width,
        Height = height,
        Tiles = tiles,
        CollisionMask = collision,
        BorderBlocked = true,
        UsePadded = false
    };

    return MapService.CreateFromTemplate(mapTemplate);
}