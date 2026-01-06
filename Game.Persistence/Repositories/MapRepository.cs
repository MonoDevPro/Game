using Game.Domain.Entities;
using Game.Persistence.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Game.Persistence.Repositories;

internal sealed class MapRepository(GameDbContext context) : Repository<Map>(context), IMapRepository
{
    public async Task<Map?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = tracking ? DbSet.AsTracking() : DbSet.AsNoTracking();
        return await query.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<Map?> GetByNameAsync(string name, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = tracking ? DbSet.AsTracking() : DbSet.AsNoTracking();
        var upper = name.ToUpperInvariant();
        return await query.FirstOrDefaultAsync(m => m.Name.ToUpper() == upper, cancellationToken);
    }
}
