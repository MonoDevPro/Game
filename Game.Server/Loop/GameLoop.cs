using Game.Core;
using Game.Server.Players;

namespace Game.Server.Loop;

public class GameLoopService : BackgroundService
{
    private readonly GameSimulation _simulation;
    private readonly PlayerStateBroadcaster _broadcaster;
    private readonly ILogger<GameLoopService> _logger;
    private const int TargetFps = 60;
    private const float TargetFrameTime = 1f / TargetFps;

    public GameLoopService(
        GameSimulation simulation,
        PlayerStateBroadcaster broadcaster,
        ILogger<GameLoopService> logger)
    {
        _simulation = simulation;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Game loop starting...");
            
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var lastTime = 0f;

        while (!stoppingToken.IsCancellationRequested)
        {
            var currentTime = (float)stopwatch.Elapsed.TotalSeconds;
            var deltaTime = currentTime - lastTime;
            lastTime = currentTime;

            try
            {
                _simulation.Update(deltaTime);
                _broadcaster.Broadcast();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in game loop");
            }

            // Sleep para manter frame rate
            var frameTime = (float)stopwatch.Elapsed.TotalSeconds - currentTime;
            var sleepTime = TargetFrameTime - frameTime;
            
            if (sleepTime > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(sleepTime), stoppingToken);
            }
        }

        _logger.LogInformation("Game loop stopped");
    }
}
