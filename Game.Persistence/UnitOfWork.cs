// Persistence/IUnitOfWork.cs
using Game.Persistence.Repositories;
using Game.Domain.Entities;
using Game.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Game.Persistence;

public class UnitOfWork(GameDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;
    
    public IRepository<Item> Accounts { get; } = new Repository<Item>(context);
    public IRepository<Item> Characters { get; } = new Repository<Item>(context);
    public IRepository<Item> Stats { get; } = new Repository<Item>(context);
    public IRepository<Item> Items { get; } = new Repository<Item>(context);
    public IRepository<Item> ItemStats { get; } = new Repository<Item>(context);
    public IRepository<Item> Inventories { get; } = new Repository<Item>(context);
    public IRepository<Item> InventorySlots { get; } = new Repository<Item>(context);
    public IRepository<Item> EquipmentSlots { get; } = new Repository<Item>(context);

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
    }
}