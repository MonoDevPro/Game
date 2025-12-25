using Game.Domain.Entities;
using Game.Persistence.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Game.Persistence.Repositories;

internal class AccountRepository(GameDbContext context) : Repository<Account>(context), IAccountRepository
{
    public async Task<Account?> GetByIdWithCharactersAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsTracking()
            .Include(a => a.Characters)
            .ThenInclude(c => c.Stats)
            .Include(a => a.Characters)
            .ThenInclude(c => c.Inventory)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Account?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsTracking()
            .FirstOrDefaultAsync(a => a.Username.ToUpper() == username.ToUpperInvariant(), cancellationToken);
    }

    public async Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(a => a.Email.ToUpper() == email.ToUpperInvariant(), cancellationToken);
    }

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(a => a.Username.ToUpper() == username.ToUpperInvariant(), cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(a => a.Email.ToUpper() == email.ToUpperInvariant(), cancellationToken);
    }

    public async Task<Account?> GetByUsernameWithCharactersAsync(string username, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsTracking()
            .Include(a => a.Characters)
            .ThenInclude(c => c.Stats)
            .Include(a => a.Characters)
            .ThenInclude(c => c.Inventory)
            .FirstOrDefaultAsync(a => a.Username == username, cancellationToken);
    }
}