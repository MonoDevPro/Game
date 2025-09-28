using GameWeb.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace GameWeb.Infrastructure.Data;

public class EfUnitOfWork(ApplicationDbContext context, ILogger<EfUnitOfWork> logger) : IUnitOfWork
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<EfUnitOfWork> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private IDbContextTransaction? _currentTransaction;
    
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    // -------------------------
    // Transaction support
    // -------------------------
    public async Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            _logger.LogDebug("EfUnitOfWork: transaction already started, reusing.");
            return false; // já tinha uma transação ativa
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogDebug("EfUnitOfWork: transaction started.");
        return true; // nós iniciamos
    }


    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            _logger.LogWarning("EfUnitOfWork: CommitTransaction called but there is no active transaction.");
            return;
        }

        try
        {
            //await _context.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
            _logger.LogDebug("EfUnitOfWork: transaction committed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EfUnitOfWork: commit failed, rolling back.");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            _logger.LogWarning("EfUnitOfWork: RollbackTransaction called but there is no active transaction.");
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
            _logger.LogDebug("EfUnitOfWork: transaction rolled back.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EfUnitOfWork: rollback failed.");
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        try
        {
            if (_currentTransaction is not null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
                _logger.LogDebug("EfUnitOfWork: transaction disposed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EfUnitOfWork: disposing transaction failed.");
            _currentTransaction = null;
        }
    }
}
