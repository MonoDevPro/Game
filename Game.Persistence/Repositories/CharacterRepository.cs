using Game.Domain.Entities;
using Game.Persistence.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Game.Persistence.Repositories;

internal class CharacterRepository(GameDbContext context) : Repository<Character>(context), ICharacterRepository
{
    public async Task<Character[]> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsTracking()
            .Include(c => c.Inventory)
            .Where(c => c.AccountId == accountId)
            .OrderBy(c => c.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Character?> GetByIdWithInventoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsTracking()
            .Include(c => c.Inventory)
            .ThenInclude(i => i.Slots)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(c => c.Name.ToUpper() == name.ToUpperInvariant(), cancellationToken);
    }

    public async Task<int> CountByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(c => c.AccountId == accountId, cancellationToken);
    }
    
    public async Task<Character?> GetByIdWithRelationsForDeletionAsync(int characterId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Inventory)
            .ThenInclude(i => i.Slots) // Incluir slots para deleção em cascata
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);
    }
}