using Simulation.Core.Options;

namespace Server.Authentication.Session;

public record SessionInfo(Guid Token, int AccountId, DateTime CreatedAt, DateTime ExpiresAt, int PeerId);

public class SessionManager(AuthOptions options, TimeProvider time)
{
    private readonly Dictionary<Guid, SessionInfo> _sessions = new();
    private readonly Dictionary<int, Guid> _peerToToken = new();

    public Guid CreateSession(int accountId, int peerId)
    {
        var token = Guid.NewGuid();
        var now = time.GetUtcNow();
        var info = new SessionInfo(token, accountId, now.DateTime, now.DateTime.AddMinutes(options.DefaultSessionLifetimeMinutes), peerId);
        _sessions[token] = info;
        _peerToToken[peerId] = token;
        return token;
    }

    public bool TryGetByToken(Guid token, out SessionInfo? info)
        => _sessions.TryGetValue(token, out info);

    public bool TryGetByPeer(int peerId, out SessionInfo? info)
    {
        info = null;
        return _peerToToken.TryGetValue(peerId, out var token) 
               && _sessions.TryGetValue(token, out info);
    }

    public void RemoveByPeer(int peerId)
    {
        if (_peerToToken.Remove(peerId, out var token))
        {
            _sessions.Remove(token, out _);
        }
    }

    // chamar periodicamente (background) para limpar expirados
    public void CleanupExpired()
    {
        var now = time.GetUtcNow();
        foreach (var kv in _sessions)
        {
            if (kv.Value.ExpiresAt <= now)
            {
                _sessions.Remove(kv.Key, out _);
                _peerToToken.Remove(kv.Value.PeerId, out _);
            }
        }
    }
}
