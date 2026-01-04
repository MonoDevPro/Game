using Microsoft.EntityFrameworkCore;
using Game.Domain.Entities;
using Game.Domain.Enums;

namespace Game.Persistence;

/// <summary>
/// Contexto do banco de dados do jogo com configurações de cascade delete otimizadas.
/// Autor: MonoDevPro
/// Data: 2025-10-13 20:18:33
/// </summary>
internal class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
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

        // Seed data
        SeedData(modelBuilder);
    }

    // ========== SEED DATA ==========

    /// <summary>
    /// Seed inicial de dados.
    /// </summary>
    private void SeedData(ModelBuilder modelBuilder)
    {
        SeedItems(modelBuilder);
    }

    private void SeedItems(ModelBuilder modelBuilder)
    {
        var items = new[]
        {
            new Item
            {
                Id = 1,
                Name = "Health Potion",
                Description = "Restaura 50 de HP",
                Type = ItemType.Consumable,
                StackSize = 99,
                Weight = 1,
                IconPath = "icons/health_potion.png",
                RequiredLevel = 1,
                RequiredVocation = null,
                IsActive = true
            },
            new Item
            {
                Id = 2,
                Name = "Iron Sword",
                Description = "Uma espada de ferro básica",
                Type = ItemType.Weapon,
                StackSize = 1,
                Weight = 10,
                IconPath = "icons/iron_sword.png",
                RequiredLevel = 5,
                RequiredVocation = VocationType.Warrior,
                IsActive = true
            },
            new Item
            {
                Id = 3,
                Name = "Leather Armor",
                Description = "Armadura leve de couro",
                Type = ItemType.Armor,
                StackSize = 1,
                Weight = 15,
                IconPath = "icons/leather_armor.png",
                RequiredLevel = 3,
                RequiredVocation = null,
                IsActive = true
            },
            new Item
            {
                Id = 4,
                Name = "Magic Staff",
                Description = "Cajado mágico para magos",
                Type = ItemType.Weapon,
                StackSize = 1,
                Weight = 8,
                IconPath = "icons/magic_staff.png",
                RequiredLevel = 5,
                RequiredVocation = VocationType.Mage,
                IsActive = true
            }
        };

        modelBuilder.Entity<Item>().HasData(items);

        // Stats dos itens equipáveis
        var itemStats = new[]
        {
            new ItemStats
            {
                Id = 1,
                ItemId = 2, // Iron Sword
                BonusStrength = 5,
                BonusPhysicalAttack = 15,
                IsActive = true
            },
            new ItemStats
            {
                Id = 2,
                ItemId = 3, // Leather Armor
                BonusConstitution = 3,
                BonusPhysicalDefense = 10,
                IsActive = true
            },
            new ItemStats
            {
                Id = 3,
                ItemId = 4, // Magic Staff
                BonusIntelligence = 8,
                BonusMagicAttack = 20,
                IsActive = true
            }
        };

        modelBuilder.Entity<ItemStats>().HasData(itemStats);
    }
}