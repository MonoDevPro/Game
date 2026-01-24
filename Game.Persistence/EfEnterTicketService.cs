using Game.Application;

namespace Game.Persistence;

public sealed class EfEnterTicketService : IEnterTicketService
{
    private readonly GameDbContext _db;
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);

    public EfEnterTicketService(GameDbContext db)
    {
        _db = db;
    }

    public string IssueTicket(int characterId)
    {
        var ticket = Guid.NewGuid().ToString("N");
        var row = new EnterTicketRow
        {
            Ticket = ticket,
            CharacterId = characterId,
            ExpiresAt = DateTimeOffset.UtcNow.Add(_ttl)
        };
        _db.EnterTickets.Add(row);
        _db.SaveChanges();
        return ticket;
    }

    public bool TryConsumeTicket(string ticket, out int characterId)
    {
        var row = _db.EnterTickets.FirstOrDefault(x => x.Ticket == ticket);
        if (row is null)
        {
            characterId = 0;
            return false;
        }

        if (row.ExpiresAt < DateTimeOffset.UtcNow)
        {
            _db.EnterTickets.Remove(row);
            _db.SaveChanges();
            characterId = 0;
            return false;
        }

        characterId = row.CharacterId;
        _db.EnterTickets.Remove(row);
        _db.SaveChanges();
        return true;
    }
}
