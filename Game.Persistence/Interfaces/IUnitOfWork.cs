using Game.Domain.Entities;
using Game.Persistence.Interfaces.Repositories;

namespace Game.Persistence.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // Repositórios
    IAccountRepository Accounts { get; }
    ICharacterRepository Characters { get; }
    
    // Transações
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}