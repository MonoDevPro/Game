using Game.Core.Maps;
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
            services.AddSingleton<ServerSimulation>();
            
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<SessionTokenManager>();
            services.AddScoped<AccountLoginService>();
            services.AddScoped<AccountRegistrationService>();
            services.AddScoped<AccountCharacterService>();
            services.AddSingleton<PlayerSpawnService>();
            services.AddSingleton<PlayerSessionManager>();
            services.AddSingleton<NetworkSecurity>(p => new NetworkSecurity(maxMessagesPerSecond: 50));
            services.AddSingleton<GameServer>();
            
            services.AddSingleton<Map>(sp =>
            {
                int width = 200;
                int height = 200;
                int layers = 1;
                var map = new Map("ExampleMap", width, height, layers, borderBlocked: true);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var tileType = (x == 0 || y == 0 || x == width - 1 || y == height - 1) ? TileType.Wall : TileType.Floor;
                        map.SetTile(x, y, 0, new Tile { Type = tileType });
                    }
                }
                return map;
            });
            
            services.AddSingleton<IMapGrid>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<MapGrid>>();
                var map = sp.GetRequiredService<Map>();
                MapGrid.SetDefaultOptions(MapGridFactoryOptions.Server);
                var mapGrid = MapGrid.Create(map, out var info);
                logger.LogInformation(info.ToString());
                return mapGrid;
            });
            
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
