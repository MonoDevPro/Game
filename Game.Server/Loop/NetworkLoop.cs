
namespace Game.Server.Loop;

public class NetworkLoopService : BackgroundService
{
    //private readonly GameServer _server;
    private readonly ILogger<NetworkLoopService> _logger;

    public NetworkLoopService(
        //GameServer server,
        ILogger<NetworkLoopService> logger)
    {
        //_server = server;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Network loop starting...");
            
        //_server.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //_server.Update();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in network loop");
            }

            await Task.Delay(15, stoppingToken); // ~66Hz
        }

        _logger.LogInformation("Network loop stopped");
    }
}