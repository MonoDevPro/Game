
namespace Game.Server.Loop;

public class NetworkLoopService( //GameServer server,
    ILogger<NetworkLoopService> logger)
    : BackgroundService
{
    //private readonly GameServer _server;

    //_server = server;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Network loop starting...");
            
        //_server.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //_server.Update();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in network loop");
            }

            await Task.Delay(15, stoppingToken); // ~66Hz
        }

        logger.LogInformation("Network loop stopped");
    }
}