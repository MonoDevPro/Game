using Game.Network.Abstractions;

namespace Game.Server.Loop;

public class NetworkLoopService(
    INetworkManager networkManager,
    GameServer gameServer,
    ILogger<NetworkLoopService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Network loop starting...");
        gameServer.Start();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    networkManager.PollEvents();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in network loop");
                }

                await Task.Delay(15, stoppingToken); // ~66Hz
            }
        }
        finally
        {
            gameServer.Stop();
            logger.LogInformation("Network loop stopped");
        }
    }
}