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
using Game.Server.Simulation.Maps;

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
            
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<SessionTokenManager>();
            services.AddScoped<AccountLoginService>();
            services.AddScoped<AccountRegistrationService>();
            services.AddScoped<AccountCharacterService>();
            services.AddSingleton<PlayerSpawnService>();
            services.AddSingleton<INpcRepository, NpcRepository>();
            services.AddSingleton<NpcSpawnService>();
            services.AddSingleton<PlayerSessionManager>();
            services.AddSingleton<ChatService>();
            services.AddSingleton(new NetworkSecurity(maxMessagesPerSecond: 50));
            services.AddSingleton<GameServer>();
            
            // Maps (DB + cache)
            services.AddSingleton<IMapCacheService, MapCacheService>();
            services.AddSingleton<IMapLoader, DbMapLoader>();

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