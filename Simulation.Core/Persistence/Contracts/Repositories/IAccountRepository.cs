using Simulation.Core.Persistence.Models;

namespace Simulation.Core.Persistence.Contracts.Repositories;

public interface IAccountRepository
{
    Task<int> CreateAsync(AccountModel model, CancellationToken ct = default);
    Task<AccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<AccountModel?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> UpdateAsync(AccountModel model, CancellationToken ct = default);
    Task<bool> UpdateLastLoginAsync(int id, CancellationToken ct = default);
}