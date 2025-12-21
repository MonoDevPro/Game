using Game.ECS.Server;

namespace Game.Server.Loop;

public class GameLoopService(
    ServerGameSimulation simulation,
    ILogger<GameLoopService> logger)
    : BackgroundService
{
    private const int TargetFps = 60;
    private const float TargetFrameTime = 1f / TargetFps;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Game loop starting...");
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var lastTime = 0f;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var currentTime = (float)stopwatch.Elapsed.TotalSeconds;
            var deltaTime = currentTime - lastTime;
            lastTime = currentTime;
            
            try
            {
                simulation.Update(deltaTime);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in game loop");
            }
            
            // Sleep para manter frame rate
            var frameTime = (float)stopwatch.Elapsed.TotalSeconds - currentTime;
            var sleepTime = TargetFrameTime - frameTime;
            
            if (sleepTime > 0)
                await Task.Delay(TimeSpan.FromSeconds(sleepTime), stoppingToken);
        }
        
        logger.LogInformation("Game loop stopped");
    }
}
