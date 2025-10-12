using System.Net;
using Game.Network.Abstractions;
using LiteNetLib.Utils;

namespace Game.Server.Security;

public class NetworkSecurity(int maxMessagesPerSecond)
{
    private readonly Dictionary<INetPeerAdapter, RateLimiter> _rateLimiters = new();
    private readonly Dictionary<IPEndPoint, RateLimiter> _unconnectedRateLimiters = new();
    
    public bool ValidateUnconnectedMessage(IPEndPoint endPoint, IPacket packet)
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

    public bool ValidateMessage(INetPeerAdapter peer, IPacket packet)
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
