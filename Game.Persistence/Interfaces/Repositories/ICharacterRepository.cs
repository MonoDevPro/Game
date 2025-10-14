using Game.Domain.Entities;

namespace Game.Persistence.Interfaces.Repositories;

public interface ICharacterRepository : IRepository<Character>
{
    Task<Character[]> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<Character?> GetByIdWithStatsAsync(int id, CancellationToken cancellationToken = default);
    Task<Character?> GetByIdWithStatsAndInventoryAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<int> CountByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<Character?> GetByIdWithRelationsForDeletionAsync(int characterId, CancellationToken cancellationToken = default);
}