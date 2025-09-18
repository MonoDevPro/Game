using Microsoft.EntityFrameworkCore;
using Server.Persistence.Context;
using Simulation.Core.Persistence.Contracts.Repositories;
using Simulation.Core.Persistence.Models;

namespace Server.Persistence.Repositories;

public class AccountRepository(SimulationDbContext context, TimeProvider provider)
    : EFCoreRepository<int, AccountModel>(context), IAccountRepository
{
    public async Task<int> CreateAsync(AccountModel model, CancellationToken ct = default)
    {
        model.CreatedAt = DateTime.UtcNow;
        await AddAsync(model, ct);
        await SaveChangesAsync(ct); // commit imediato para retornar Id
        return model.Id;
    }

    public async Task<AccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        // exemplo simples: case-sensitive depende do DB collation; considere ToLower invariants se desejar insensibilidade
        var spec = new SimpleSpecification<AccountModel>(a => a.Username == username);
        var list = await FindAsync(spec, ct);
        return list.Count > 0 ? list[0] : null;
    }

    public Task<AccountModel?> GetByIdAsync(int id, CancellationToken ct = default) => GetAsync(id, ct);

    public async Task<bool> UpdateAsync(AccountModel model, CancellationToken ct = default)
    {
        var success = await UpdateAsync(model.Id, model, ct);
        if (!success) return false;
        await SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateLastLoginAsync(int id, CancellationToken ct = default)
    {
        var (found, entity) = await TryGetAsync(id, ct);
        if (!found || entity == null) return false;
        entity.LastLoginAt = provider.GetUtcNow().DateTime;
        Context.Update(entity);
        await SaveChangesAsync(ct);
        return true;
    }
}