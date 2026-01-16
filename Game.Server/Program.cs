using Game.ECS.Services.Map;
using Game.Network;
using Game.Network.Abstractions;
using Game.Persistence;
using Game.Server;
using Game.Server.Authentication;
using Game.Server.Chat;
using Game.Server.Loop;
using Game.Server.Players;
using Game.Server.Npc;
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
            services.AddSingleton<ServerGameSimulation>();
            services.AddSingleton<WorldMapRegistry>(sp =>
            {
                var registry = new WorldMapRegistry();
                WorldMap[] maps =
                [
                    new(1, "Starter Village", 100, 100, 3),
                    new(2, "Forest of Beginnings", 200, 200, 2),
                    new(3, "Cave of Trials", 150, 150, 4)
                ];
                registry.RegisterRange(maps);
                return registry;
            });
            
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<SessionTokenManager>();
            services.AddScoped<AccountLoginService>();
            services.AddScoped<AccountRegistrationService>();
            services.AddScoped<AccountCharacterService>();
            services.AddSingleton<PlayerSpawnService>();
            services.AddSingleton<INpcRepository, NpcRepository>();
            services.AddSingleton<PlayerSessionManager>();
            services.AddSingleton<ChatService>();
            services.AddSingleton(new NetworkSecurity(maxMessagesPerSecond: 50));
            services.AddSingleton<GameServer>();
            
            // Network
            //services.AddSingleton<GameServer>();
            services.AddNetworking(new NetworkOptions
            {
                IsServer = true,
                ServerAddress = "127.0.0.1",
                ServerPort = 8001,
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