using Game.Application;

namespace Game.Infrastructure.EfCore;

public sealed class EfEnterTicketService(GameDbContext db) : IEnterTicketService
{
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);

    public string IssueTicket(int characterId)
    {
        var ticket = Guid.NewGuid().ToString("N");
        var row = new EnterTicketRow
        {
            Ticket = ticket,
            CharacterId = characterId,
            ExpiresAt = DateTimeOffset.UtcNow.Add(_ttl)
        };
        db.EnterTickets.Add(row);
        db.SaveChanges();
        return ticket;
    }

    public bool TryConsumeTicket(string ticket, out int characterId)
    {
        var row = db.EnterTickets.FirstOrDefault(x => x.Ticket == ticket);
        if (row is null)
        {
            characterId = 0;
            return false;
        }

        if (row.ExpiresAt < DateTimeOffset.UtcNow)
        {
            db.EnterTickets.Remove(row);
            db.SaveChanges();
            characterId = 0;
            return false;
        }

        characterId = row.CharacterId;
        db.EnterTickets.Remove(row);
        db.SaveChanges();
        return true;
    }
}
