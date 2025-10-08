
namespace Game.Server.Security;

public class RateLimiter(int maxMessagesPerSecond)
{
    private readonly Queue<float> _messageTimes = new();

    public bool AllowMessage()
    {
        var currentTime = Time.GetTime();
            
        // Remover mensagens antigas (> 1 segundo)
        while (_messageTimes.Count > 0 && currentTime - _messageTimes.Peek() > 1f)
            _messageTimes.Dequeue();

        if (_messageTimes.Count >= maxMessagesPerSecond)
            return false;

        _messageTimes.Enqueue(currentTime);
        return true;
    }
    
    public static class Time
    {
        private static readonly System.Diagnostics.Stopwatch Stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        public static float GetTime() => (float)Stopwatch.Elapsed.TotalSeconds;
    }
}