using Game.Persistence.Interfaces;
using Game.Persistence.Interfaces.Repositories;
using Game.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Game.Persistence;

/// <summary>
/// Unit of Work pattern implementation.
/// Centralizes repository access and transaction management.
/// 
/// Author: MonoDevPro
/// Date: 2025-10-13
/// </summary>
public class UnitOfWork(GameDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;
    
    public IAccountRepository Accounts { get; } = new AccountRepository(context);
    public ICharacterRepository Characters { get; } = new CharacterRepository(context);

    // ========== TRANSACTION MANAGEMENT ==========
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context?.Dispose();
        GC.SuppressFinalize(this);
    }
}