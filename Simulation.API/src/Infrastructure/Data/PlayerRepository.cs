using Application.Abstractions.Commons;
using GameWeb.Application.Characters.Services;
using GameWeb.Application.Common;
using GameWeb.Domain.Entities;
using GameWeb.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace GameWeb.Infrastructure.Data;

public class PlayerRepository(ApplicationDbContext dbContext) : IPlayerRepository
{
    #region Leitura (Queries)
    
    public async Task<Player?> GetPlayerAsync(int id, bool asNoTracking = true, bool ignoreFilters = false, CancellationToken ct = default)
    {
        var query = GetQuery(asNoTracking, ignoreFilters);
        return await query.FirstOrDefaultAsync(p => p.Id == id, ct);
    }
    
    public async Task<Player?> GetPlayerAsync(string name, bool asNoTracking = true, bool ignoreFilters = false, CancellationToken ct = default)
    {
        var query = GetQuery(asNoTracking, ignoreFilters);
        return await query.FirstOrDefaultAsync(p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase), ct);
    }

    public Task<List<Player>> GetPlayersByUserIdAsync(string userId, bool asNoTracking = true, bool ignoreFilters = false,
        CancellationToken ct = default)
    {
        var query = GetQuery(asNoTracking, ignoreFilters);
        return query.Where(p => p.UserId == userId).ToListAsync(ct);
    }

    public async Task<IPaginatedList<Player>> ListPagedAsync(int pageNumber, int pageSize, bool asNoTracking = true, bool ignoreFilters = false, CancellationToken ct = default)
    {
        var query = GetQuery(asNoTracking, ignoreFilters)
            .OrderBy(p => p.Name); // Ordenação é crucial para paginação consistente

        return await PaginatedList<Player>.CreateAsync(query, pageNumber, pageSize, ct);
    }
    
    public async Task<bool> ExistPlayerAsync(string name, bool ignoreFilters = true, CancellationToken ct = default)
    {
        // Métodos como Any/Count não precisam de AsNoTracking, mas ainda respeitam IgnoreQueryFilters
        IQueryable<Player> query = dbContext.Players;
        if (ignoreFilters)
        {
            query = query.IgnoreQueryFilters();
        }
        return await query.AnyAsync(p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase), ct);
    }

    public async Task<long> CountMyPlayersAsync(string userId, bool ignoreFilters = true, CancellationToken ct = default)
    {
        IQueryable<Player> query = dbContext.Players;
        if (ignoreFilters)
        {
            query = query.IgnoreQueryFilters();
        }
        return await query.LongCountAsync(p => p.UserId == userId, ct);
    }

    public async Task<long> CountAllPlayersAsync(bool ignoreFilters = false, CancellationToken ct = default)
    {
        IQueryable<Player> query = dbContext.Players;
        if (ignoreFilters)
        {
            query = query.IgnoreQueryFilters();
        }
        return await query.LongCountAsync(ct);
    }

    #endregion

    #region Escrita (Commands / Unit of Work)

    public void CreatePlayer(Player player)
    {
        dbContext.Players.Add(player);
    }

    public void Update(Player player)
    {
        dbContext.Players.Update(player);
    }
    
    public void Deactivate(Player player)
    {
        player.IsActive = false;
        dbContext.Players.Update(player);
    }
    
    public void Reactivate(Player player)
    {
        player.IsActive = true;
        dbContext.Players.Update(player);
    }

    public void Delete(Player player)
    {
        dbContext.Players.Remove(player);
    }
    
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await dbContext.SaveChangesAsync(ct);
    }

    #endregion

    #region Métodos Auxiliares
    
    /// <summary>
    /// Cria uma IQueryable<Player> base, aplicando condicionalmente AsNoTracking e IgnoreQueryFilters.
    /// </summary>
    /// <param name="asNoTracking">Se true, a query não rastreará as entidades retornadas (melhor para leitura).</param>
    /// <param name="ignoreFilters">Se true, ignora filtros de query globais (ex: para ver itens com soft-delete).</param>
    /// <returns>Uma IQueryable<Player> configurada.</returns>
    private IQueryable<Player> GetQuery(bool asNoTracking = true, bool ignoreFilters = false)
    {
        IQueryable<Player> query = dbContext.Players;

        if (ignoreFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query;
    }

    #endregion
}
