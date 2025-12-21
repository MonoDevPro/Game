using System.Net;
using Game.Network.Abstractions;

namespace Game.Server.Security;

public class NetworkSecurity(int maxMessagesPerSecond)
{
    private readonly Dictionary<INetPeerAdapter, RateLimiter> _rateLimiters = new();
    private readonly Dictionary<IPEndPoint, RateLimiter> _unconnectedRateLimiters = new();
    
    public bool ValidateUnconnectedMessage<T>(IPEndPoint endPoint, ref T _) where T : struct
    {
        // 1. Rate limiting
        if (!_unconnectedRateLimiters.TryGetValue(endPoint, out var limiter))
        {
            limiter = new RateLimiter(maxMessagesPerSecond);
            _unconnectedRateLimiters[endPoint] = limiter;
        }

        if (!limiter.AllowMessage())
            return false;

        return true;
    }

    public bool ValidateMessage<T>(INetPeerAdapter peer, ref T _) where T : struct
    {
        // 1. Rate limiting
        if (!_rateLimiters.TryGetValue(peer, out var limiter))
        {
            limiter = new RateLimiter(maxMessagesPerSecond);
            _rateLimiters[peer] = limiter;
        }

        if (!limiter.AllowMessage())
        {
            RemovePeer(peer);
            peer.Disconnect();
            return false;
        }

        return true;
    }

    public void RemovePeer(INetPeerAdapter peer)
    {
        _rateLimiters.Remove(peer);
    }
    
    public void ClearUnconnectedLimiter(IPEndPoint endPoint)
    {
        _unconnectedRateLimiters.Remove(endPoint);
    }
}
