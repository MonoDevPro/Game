using System.Collections.Concurrent;
using Game.Application;

namespace Game.Infrastructure.Shared;

public sealed class InMemorySessionService : ISessionService
{
    private readonly ConcurrentDictionary<string, int> _sessions = new(StringComparer.Ordinal);

    public string CreateSession(int accountId)
    {
        var token = Guid.NewGuid().ToString("N");
        _sessions[token] = accountId;
        return token;
    }

    public bool TryGetAccountId(string token, out int accountId)
        => _sessions.TryGetValue(token, out accountId);
}
