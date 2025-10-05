using Game.Domain.Entities;

namespace Game.Persistence.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // Repositórios
    IRepository<Item> Accounts { get; }
    IRepository<Item> Characters { get; }
    IRepository<Item> Stats { get; }
    IRepository<Item> Items { get; }
    IRepository<Item> ItemStats { get; }
    IRepository<Item> Inventories { get; }
    IRepository<Item> InventorySlots { get; }
    IRepository<Item> EquipmentSlots { get; }
    
    // Transações
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}