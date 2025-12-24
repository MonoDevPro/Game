using Game.Domain.Player;

namespace Game.Persistence.Interfaces.Repositories;

// 1. AccountRepository
public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetByIdWithCharactersAsync(int id, CancellationToken cancellationToken = default);
    Task<Account?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Account?> GetByUsernameWithCharactersAsync(string username, CancellationToken cancellationToken = default);
}