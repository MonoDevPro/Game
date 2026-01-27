using Game.Application;
using Game.Domain;
using Microsoft.EntityFrameworkCore;

namespace Game.Infrastructure.EfCore;

public sealed class EfAccountRepository(GameDbContext db) : IAccountRepository
{
    public async Task<Account?> FindByUsernameAsync(string username, CancellationToken ct = default)
    {
        var row = await db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username, ct);
        return row is null ? null : new Account(row.Id, row.Username, row.Email, row.PasswordHash);
    }
}

public sealed class EfCharacterRepository(GameDbContext db) : ICharacterRepository
{
    public async Task<IReadOnlyList<Character>> ListByAccountIdAsync(int accountId, CancellationToken ct = default)
    {
        var rows = await db.Characters.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<Character?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        var row = await db.Characters.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task UpdateAsync(Character character, CancellationToken ct = default)
    {
        var row = await db.Characters.FirstOrDefaultAsync(x => x.Id == character.Id, ct);
        if (row is null) return;

        row.X = character.X;
        row.Y = character.Y;
        row.Floor = character.Floor;
        row.DirX = character.DirX;
        row.DirY = character.DirY;
        row.HealthPoints = character.HealthPoints;
        row.ManaPoints = character.ManaPoints;
        await db.SaveChangesAsync(ct);
    }

    private static Character ToDomain(CharacterRow row)
        => new(row.Id, row.AccountId, row.Name, (Gender)row.Gender, row.X, row.Y, row.Floor, row.DirX, row.DirY,
            (Vocation)row.Vocation, row.Level, row.Experience, row.Strength, row.Endurance, row.Agility,
            row.Intelligence, row.Willpower, row.HealthPoints, row.ManaPoints);
}
