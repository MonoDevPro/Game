using Application.Abstractions.Commons;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Characters.Services;

public interface IPlayerRepository
{
    // Leitura direta do banco com controle de tracking e filtros
    Task<Player?> GetPlayerAsync(int id, bool asNoTracking = true, bool ignoreFilters = false, CancellationToken ct = default);
    Task<Player?> GetPlayerAsync(string name, bool asNoTracking = true, bool ignoreFilters = false, CancellationToken ct = default);
    Task<List<Player>> GetPlayersByUserIdAsync(string userId, bool asNoTracking = true, bool ignoreFilters = false, CancellationToken ct = default);
    Task<IPaginatedList<Player>> ListPagedAsync(int pageNumber, int pageSize, bool asNoTracking = true, bool ignoreFilters = false, CancellationToken ct = default);
    
    // Métodos que não retornam entidades não precisam de controle de tracking
    Task<bool> ExistPlayerAsync(string name, bool ignoreFilters = true, CancellationToken ct = default);
    Task<long> CountMyPlayersAsync(string userId, bool ignoreFilters = true, CancellationToken ct = default);
    Task<long> CountAllPlayersAsync(bool ignoreFilters = false, CancellationToken ct = default);

    // Métodos de escrita (Unit of Work)
    void CreatePlayer(Player player);
    void Update(Player player);
    void Deactivate(Player player);
    void Reactivate(Player player);
    void Delete(Player player);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
