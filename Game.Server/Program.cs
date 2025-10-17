using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.ECS.Services;
using Game.Network;
using Game.Network.Abstractions;
using Game.Persistence;
using Game.Server;
using Game.Server.Authentication;
using Game.Server.Loop;
using Game.Server.Players;
using Game.Server.Security;
using Game.Server.Sessions;
using Game.Server.Simulation;
using Game.Server.Simulation.Utils;

var builder = CreateHostBuilder(args);

var host = builder.Build();
host.Run();

return;

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            var configuration = hostContext.Configuration;
                    
            // Database
            services.AddGameDatabase(
                configuration.GetConnectionString("GameDatabase") ?? throw new InvalidOperationException("Connection string 'GameDatabase' not found."));
            
            services.AddSingleton(TimeProvider.System);
            
            // ECS
            services.AddSingleton<GameSimulation>();
            
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<SessionTokenManager>();
            services.AddScoped<AccountLoginService>();
            services.AddScoped<AccountRegistrationService>();
            services.AddScoped<AccountCharacterService>();
            services.AddSingleton<PlayerSpawnService>();
            services.AddSingleton<PlayerSessionManager>();
            services.AddSingleton<NetworkSecurity>(p => new NetworkSecurity(maxMessagesPerSecond: 50));
            services.AddSingleton<GameServer>();
            services.AddSingleton<MapService>(CreateMapService());
                    
            // Network
            //services.AddSingleton<GameServer>();
            services.AddNetworking(new NetworkOptions
            {
                IsServer = true,
                ServerAddress = "127.0.0.1",
                ServerPort = 7777,
                ConnectionKey = "default",
                PingIntervalMs = 2000,
                DisconnectTimeoutMs = 5000,
            });
            
            // Background services
            services.AddHostedService<DatabaseMigrationService>();
            services.AddHostedService<GameLoopService>();
            services.AddHostedService<NetworkLoopService>();
            
            // Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
        
        
static MapService CreateMapService()
{
    const int width = 16;
    const int height = 16;
    var tiles = new TileType[width * height];
    for (var i = 0; i < tiles.Length; i++)
    {
        tiles[i] = TileType.Floor;
    }

    var template = new Map
    {
        Id = 1,
        Name = "TestMap",
        Width = width,
        Height = height,
        Tiles = tiles,
        CollisionMask = new byte[width * height],
        BorderBlocked = false,
        UsePadded = false
    };

    return template.CreateFromEntity();
}