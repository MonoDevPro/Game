using Game.Core;
using Game.Core.Services;
using Game.Network.Security;
using Game.Persistence;
using Game.Server;
using Game.Server.Loop;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();


static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            var configuration = hostContext.Configuration;
                    
            // Database
            services.AddGameDatabase(
                configuration.GetConnectionString("GameDatabase") ?? throw new InvalidOperationException("Connection string 'GameDatabase' not found."));
                    
            // ECS
            services.AddSingleton<GameSimulation>();
                    
            // Network
            //services.AddSingleton<GameServer>();
            //services.AddSingleton<INetworkService, NetworkService>();
            services.AddSingleton<NetworkSecurity>();
                    
            // Services
            services.AddScoped<IPlayerService, PlayerService>();
            services.AddScoped<IInventoryService, InventoryService>();
                    
            // Background services
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