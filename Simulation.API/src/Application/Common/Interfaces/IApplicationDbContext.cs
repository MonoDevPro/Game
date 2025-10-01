using GameWeb.Domain.Entities;

namespace GameWeb.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Map> Maps { get; }
    DbSet<Player> Players { get; }
    
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
