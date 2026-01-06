using Microsoft.EntityFrameworkCore;
using Game.Domain.Entities;
using Game.Domain.Enums;

namespace Game.Persistence;

/// <summary>
/// Contexto do banco de dados do jogo com configurações de cascade delete otimizadas.
/// Autor: MonoDevPro
/// Data: 2025-10-13 20:18:33
/// </summary>
public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    // ========== DBSETS ==========
    
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Character> Characters { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<ItemStats> ItemStats { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<InventorySlot> InventorySlots { get; set; }
    public DbSet<EquipmentSlot> EquipmentSlots { get; set; }
    public DbSet<Map> Maps { get; set; }

    // ========== MODEL CONFIGURATION ==========
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameDbContext).Assembly);
    }
}