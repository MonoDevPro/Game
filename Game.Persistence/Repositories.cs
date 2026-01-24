using Game.Application;
using Game.Domain;
using Microsoft.EntityFrameworkCore;

namespace Game.Persistence;

public sealed class EfAccountRepository(GameDbContext db) : IAccountRepository
{
    public async Task<Account?> FindByUsernameAsync(string username, CancellationToken ct = default)
    {
        var row = await db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username, ct);
        return row is null ? null : new Account(row.Id, row.Username, row.Email, row.PasswordHash);
    }
}

public sealed class EfCharacterRepository : ICharacterRepository
{
    private readonly GameDbContext _db;

    public EfCharacterRepository(GameDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Character>> ListByAccountIdAsync(int accountId, CancellationToken ct = default)
    {
        var rows = await _db.Characters.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<Character?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        var row = await _db.Characters.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task UpdateAsync(Character character, CancellationToken ct = default)
    {
        var row = await _db.Characters.FirstOrDefaultAsync(x => x.Id == character.Id, ct);
        if (row is null)
        {
            return;
        }

        row.X = character.X;
        row.Y = character.Y;
        await _db.SaveChangesAsync(ct);
    }

    private static Character ToDomain(CharacterRow row)
        => new(row.Id, row.AccountId, row.Name, (Gender)row.Gender, row.X, row.Y, (Direction)row.Direction);
}

public sealed class EfCharacterVocationRepository : ICharacterVocationRepository
{
    private readonly GameDbContext _db;

    public EfCharacterVocationRepository(GameDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CharacterVocation>> ListByAccountIdAsync(int accountId, CancellationToken ct = default)
    {
        var rows = await _db.Characters.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .Select(x => x.Id)
            .Join(_db.CharacterVocations.AsNoTracking(),
                characterId => characterId,
                vocation => vocation.CharacterId,
                (_, vocation) => vocation)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<CharacterVocation?> FindByCharacterIdAsync(int characterId, CancellationToken ct = default)
    {
        var row = await _db.CharacterVocations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CharacterId == characterId, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<CharacterVocation?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        var row = await _db.CharacterVocations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task UpdateAsync(CharacterVocation vocation, CancellationToken ct = default)
    {
        var row = await _db.CharacterVocations.FirstOrDefaultAsync(x => x.Id == vocation.Id, ct);
        if (row is null)
        {
            return;
        }

        row.Level = vocation.Level;
        row.Experience = vocation.Experience;
        await _db.SaveChangesAsync(ct);
    }

    private static CharacterVocation ToDomain(CharacterVocationRow row)
        => new(row.Id, row.CharacterId, (Vocation)row.Vocation, row.Level, row.Experience, row.Strength, row.Endurance, row.Agility, row.Intelligence, row.Willpower, row.HealthPoints, row.ManaPoints);
}
