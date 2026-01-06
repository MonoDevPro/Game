using Game.Domain.Entities;

namespace Game.Persistence.Interfaces.Repositories;

public interface IMapRepository : IRepository<Map>
{
    Task<Map?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default);
    Task<Map?> GetByNameAsync(string name, bool tracking, CancellationToken cancellationToken = default);
}
