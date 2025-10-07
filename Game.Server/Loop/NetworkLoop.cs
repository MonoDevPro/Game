using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Abstractions.Network;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Game.Server.Loop;

public class NetworkLoopService : BackgroundService
{
    private readonly INetworkManager _networkManager;
    private readonly GameServer _gameServer;
    private readonly ILogger<NetworkLoopService> _logger;

    public NetworkLoopService(
        INetworkManager networkManager,
        GameServer gameServer,
        ILogger<NetworkLoopService> logger)
    {
        _networkManager = networkManager;
        _gameServer = gameServer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Network loop starting...");
        _gameServer.Start();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _networkManager.PollEvents();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in network loop");
                }

                await Task.Delay(15, stoppingToken); // ~66Hz
            }
        }
        finally
        {
            _gameServer.Stop();
            _logger.LogInformation("Network loop stopped");
        }
    }
}