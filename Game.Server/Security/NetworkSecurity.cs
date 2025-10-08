using Game.Abstractions.Network;
using LiteNetLib.Utils;

namespace Game.Server.Security;

public class NetworkSecurity(int maxMessagesPerSecond)
{
    private readonly Dictionary<INetPeerAdapter, RateLimiter> _rateLimiters = new();

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
}
