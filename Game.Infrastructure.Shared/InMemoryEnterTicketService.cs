using System.Collections.Concurrent;
using Game.Application;

namespace Game.Infrastructure.Shared;

public sealed class InMemoryEnterTicketService : IEnterTicketService
{
    private readonly ConcurrentDictionary<string, int> _tickets = new(StringComparer.Ordinal);

    public string IssueTicket(int characterId)
    {
        var ticket = Guid.NewGuid().ToString("N");
        _tickets[ticket] = characterId;
        return ticket;
    }

    public bool TryConsumeTicket(string ticket, out int characterId)
    {
        if (_tickets.TryRemove(ticket, out characterId))
        {
            return true;
        }

        characterId = 0;
        return false;
    }
}
