using System.Security.Cryptography;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;

namespace Game.Network.Security;

public class NetworkSecurity
{
    private const int MaxMessageSize = 1024 * 64; // 64KB
    private const int MaxMessagesPerSecond = 100;
    private readonly Dictionary<NetPeer, RateLimiter> _rateLimiters;
    private readonly string _connectionKey;
    private readonly ILogger<NetworkSecurity> _logger;

    public NetworkSecurity(string connectionKey, ILogger<NetworkSecurity> logger)
    {
        _connectionKey = connectionKey;
        _logger = logger;
        _rateLimiters = new Dictionary<NetPeer, RateLimiter>();
    }
    
    public void ValidateConnectionRequest(ConnectionRequest request)
    {
        if (request.AcceptIfKey(_connectionKey) is null)
            _logger.LogWarning("Invalid connection key from {RemoteEndPoint}", request.RemoteEndPoint);
    }

    public bool ValidateMessage(NetPeer peer, NetDataReader reader)
    {
        // 1. Validar tamanho
        if (reader.AvailableBytes > MaxMessageSize)
        {
            _logger.LogWarning("Message too large from {PeerAddress}: {MessageSize} bytes", peer.Address, reader.AvailableBytes);
            RemovePeer(peer);
            peer.Disconnect();
            return false;
        }

        // 2. Rate limiting
        if (!_rateLimiters.TryGetValue(peer, out var limiter))
        {
            limiter = new RateLimiter(MaxMessagesPerSecond);
            _rateLimiters[peer] = limiter;
        }

        if (!limiter.AllowMessage())
        {
            _logger.LogWarning("Rate limit exceeded for {PeerAddress}", peer.Address);
            RemovePeer(peer);
            peer.Disconnect();
            return false;
        }

        return true;
    }

    public void RemovePeer(NetPeer peer)
    {
        _rateLimiters.Remove(peer);
    }
}
