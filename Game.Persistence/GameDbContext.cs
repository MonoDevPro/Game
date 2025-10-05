// Persistence/GameDbContext.cs
using Microsoft.EntityFrameworkCore;
using Game.Domain.Entities;
using Game.Domain.Enums;

namespace Game.Persistence;

/// <summary>
/// Contexto do banco de dados do jogo
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:16:27
/// </summary>
public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Character> Characters { get; set; }
    public DbSet<Stats> Stats { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<ItemStats> ItemStats { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<InventorySlot> InventorySlots { get; set; }
    public DbSet<EquipmentSlot> EquipmentSlots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar entidades
        ConfigureAccount(modelBuilder);
        ConfigureCharacter(modelBuilder);
        ConfigureStats(modelBuilder);
        ConfigureItem(modelBuilder);
        ConfigureItemStats(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigureInventorySlot(modelBuilder);
        ConfigureEquipmentSlot(modelBuilder);

        // Seed data
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Configuração da entidade Account
    /// Autor: MonoDevPro
    /// Data: 2025-10-05 21:16:27
    /// </summary>
    private void ConfigureAccount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            // Chave primária
            entity.HasKey(e => e.Id);

            // Configuração de tabela
            entity.ToTable("Accounts");

            // Índices
            entity.HasIndex(e => e.Username)
                .IsUnique()
                .HasDatabaseName("IX_Accounts_Username");

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_Accounts_Email");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Accounts_IsActive");

            // Propriedades
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.PasswordSalt)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.LastUpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // Relacionamentos
            // Account (1) -> Characters (N)
            entity.HasMany(e => e.Characters)
                .WithOne(c => c.Account)
                .HasForeignKey(c => c.AccountId)
                .OnDelete(DeleteBehavior.Cascade) // Deletar conta = deletar personagens
                .HasConstraintName("FK_Characters_Account");
        });
    }

    /// <summary>
    /// Configuração da entidade Character
    /// Autor: MonoDevPro
    /// Data: 2025-10-05 21:16:27
    /// </summary>
    private void ConfigureCharacter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Character>(entity =>
        {
            // Chave primária
            entity.HasKey(e => e.Id);

            // Configuração de tabela
            entity.ToTable("Characters");

            // Índices
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_Characters_Name");

            entity.HasIndex(e => e.AccountId)
                .HasDatabaseName("IX_Characters_AccountId");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Characters_IsActive");

            entity.HasIndex(e => new { e.PositionX, e.PositionY })
                .HasDatabaseName("IX_Characters_Position");

            // Propriedades
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Gender)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Vocation)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Direction)
                .HasConversion<string>()
                .HasMaxLength(10);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.LastUpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // Relacionamentos
            // Character (1) -> Inventory (1)
            entity.HasOne(e => e.Inventory)
                .WithOne(i => i.Character)
                .HasForeignKey<Inventory>(i => i.CharacterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Inventory_Character");

            // Character (1) -> Stats (1)
            entity.HasOne(e => e.Stats)
                .WithOne(s => s.Character)
                .HasForeignKey<Stats>(s => s.CharacterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Stats_Character");

            // Character (1) -> EquipmentSlots (N)
            entity.HasMany(e => e.Equipment)
                .WithOne(eq => eq.Character)
                .HasForeignKey(eq => eq.CharacterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_EquipmentSlots_Character");
        });
    }

    /// <summary>
    /// Configuração da entidade Stats
    /// Autor: MonoDevPro
    /// Data: 2025-10-05 21:16:27
    /// </summary>
    private void ConfigureStats(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Stats>(entity =>
        {
            // Chave primária
            entity.HasKey(e => e.Id);

            // Configuração de tabela
            entity.ToTable("Stats");

            // Índices
            entity.HasIndex(e => e.CharacterId)
                .IsUnique()
                .HasDatabaseName("IX_Stats_CharacterId");

            entity.HasIndex(e => e.Level)
                .HasDatabaseName("IX_Stats_Level");

            // Propriedades
            entity.Property(e => e.Level)
                .HasDefaultValue(1);

            entity.Property(e => e.Experience)
                .HasDefaultValue(0);

            entity.Property(e => e.BaseStrength)
                .HasDefaultValue(5);

            entity.Property(e => e.BaseDexterity)
                .HasDefaultValue(5);

            entity.Property(e => e.BaseIntelligence)
                .HasDefaultValue(5);

            entity.Property(e => e.BaseConstitution)
                .HasDefaultValue(5);

            entity.Property(e => e.BaseSpirit)
                .HasDefaultValue(5);

            entity.Property(e => e.CurrentHp)
                .HasDefaultValue(50);

            entity.Property(e => e.CurrentMp)
                .HasDefaultValue(30);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.LastUpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // Ignorar propriedades NotMapped
            entity.Ignore(e => e.TotalStrength);
            entity.Ignore(e => e.TotalDexterity);
            entity.Ignore(e => e.TotalIntelligence);
            entity.Ignore(e => e.TotalConstitution);
            entity.Ignore(e => e.TotalSpirit);
            entity.Ignore(e => e.BonusStrength);
            entity.Ignore(e => e.BonusDexterity);
            entity.Ignore(e => e.BonusIntelligence);
            entity.Ignore(e => e.BonusConstitution);
            entity.Ignore(e => e.BonusSpirit);
            entity.Ignore(e => e.MaxHp);
            entity.Ignore(e => e.MaxMp);
            entity.Ignore(e => e.PhysicalAttack);
            entity.Ignore(e => e.MagicAttack);
            entity.Ignore(e => e.PhysicalDefense);
            entity.Ignore(e => e.MagicDefense);
            entity.Ignore(e => e.AttackSpeed);
            entity.Ignore(e => e.MovementSpeed);
        });
    }

    /// <summary>
    /// Configuração da entidade Item
    /// Autor: MonoDevPro
    /// Data: 2025-10-05 21:16:27
    /// </summary>
    private void ConfigureItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            // Chave primária
            entity.HasKey(e => e.Id);

            // Configuração de tabela
            entity.ToTable("Items");

            // Índices
            entity.HasIndex(e => e.Name)
                .HasDatabaseName("IX_Items_Name");

            entity.HasIndex(e => e.Type)
                .HasDatabaseName("IX_Items_Type");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Items_IsActive");

            // Propriedades
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.IconPath)
                .HasMaxLength(200);

            entity.Property(e => e.StackSize)
                .HasDefaultValue(1);

            entity.Property(e => e.Weight)
                .HasDefaultValue(0);

            entity.Property(e => e.RequiredLevel)
                .HasDefaultValue(1);

            entity.Property(e => e.RequiredVocation)
                .HasConversion<string?>()
                .HasMaxLength(20);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.LastUpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // Relacionamentos
            // Item (1) -> ItemStats (0..1)
            entity.HasOne(e => e.Stats)
                .WithOne(s => s.Item)
                .HasForeignKey<ItemStats>(s => s.ItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false)
                .HasConstraintName("FK_ItemStats_Item");
        });
    }

    /// <summary>
    /// Configuração da entidade ItemStats
    /// Autor: MonoDevPro
    /// Data: 2025-10-05 21:16:27
    /// </summary>
    private void ConfigureItemStats(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ItemStats>(entity =>
        {
            // Chave primária
            entity.HasKey(e => e.Id);

            // Configuração de tabela
            entity.ToTable("ItemStats");

            // Índices
            entity.HasIndex(e => e.ItemId)
                .IsUnique()
                .HasDatabaseName("IX_ItemStats_ItemId");

            // Propriedades - valores padrão 0
            entity.Property(e => e.BonusStrength).HasDefaultValue(0);
            entity.Property(e => e.BonusDexterity).HasDefaultValue(0);
            entity.Property(e => e.BonusIntelligence).HasDefaultValue(0);
            entity.Property(e => e.BonusConstitution).HasDefaultValue(0);
            entity.Property(e => e.BonusSpirit).HasDefaultValue(0);
            entity.Property(e => e.BonusPhysicalAttack).HasDefaultValue(0);
            entity.Property(e => e.BonusMagicAttack).HasDefaultValue(0);
            entity.Property(e => e.BonusPhysicalDefense).HasDefaultValue(0);
            entity.Property(e => e.BonusMagicDefense).HasDefaultValue(0);
            entity.Property(e => e.BonusAttackSpeed).HasDefaultValue(0f);
            entity.Property(e => e.BonusMovementSpeed).HasDefaultValue(0f);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.LastUpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });
    }

    /// <summary>
    /// Configuração da entidade Inventory
    /// Autor: MonoDevPro
    /// Data: 2025-10-05 21:16:27
    /// </summary>
    private void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inventory>(entity =>
        {
            // Chave primária
            entity.HasKey(e => e.Id);

            // Configuração de tabela
            entity.ToTable("Inventories");

            // Índices
            entity.HasIndex(e => e.CharacterId)
                .IsUnique()
                .HasDatabaseName("IX_Inventories_CharacterId");

            // Propriedades
            entity.Property(e => e.Capacity)
                .HasDefaultValue(30);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.LastUpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // Relacionamentos
            // Inventory (1) -> InventorySlots (N)
            entity.HasMany(e => e.Slots)
                .WithOne(s => s.Inventory)
                .HasForeignKey(s => s.InventoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_InventorySlots_Inventory");
        });
    }

    /// <summary>
    /// Configuração da entidade InventorySlot
    /// Autor: MonoDevPro
    /// Data: 2025-10-05 21:16:27
    /// </summary>
    private void ConfigureInventorySlot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventorySlot>(entity =>
        {
            // Chave primária
            entity.HasKey(e => e.Id);

            // Configuração de tabela
            entity.ToTable("InventorySlots");

            // Índices
            // Um inventário não pode ter dois slots no mesmo índice
            entity.HasIndex(e => new { e.InventoryId, e.SlotIndex })
                .IsUnique()
                .HasDatabaseName("IX_InventorySlots_InventoryId_SlotIndex");

            entity.HasIndex(e => e.ItemId)
                .HasDatabaseName("IX_InventorySlots_ItemId");

            // Propriedades
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.LastUpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // Relacionamentos
            // InventorySlot (N) -> Item (0..1)
            entity.HasOne(e => e.Item)
                .WithMany()
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Restrict) // Não deletar item se houver slots usando
                .IsRequired(false)
                .HasConstraintName("FK_InventorySlots_Item");
        });
    }

    /// <summary>
    /// Configuração da entidade EquipmentSlot
    /// Autor: MonoDevPro
    /// Data: 2025-10-05 21:16:27
    /// </summary>
    private void ConfigureEquipmentSlot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EquipmentSlot>(entity =>
        {
            // Chave primária
            entity.HasKey(e => e.Id);

            // Configuração de tabela
            entity.ToTable("EquipmentSlots");

            // Índices
            // Um personagem não pode ter dois itens no mesmo slot
            entity.HasIndex(e => new { e.CharacterId, e.SlotType })
                .IsUnique()
                .HasDatabaseName("IX_EquipmentSlots_CharacterId_SlotType");

            entity.HasIndex(e => e.ItemId)
                .HasDatabaseName("IX_EquipmentSlots_ItemId");

            // Propriedades
            entity.Property(e => e.SlotType)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.LastUpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // Relacionamentos
            // EquipmentSlot (N) -> Item (0..1)
            entity.HasOne(e => e.Item)
                .WithMany()
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Restrict) // Não deletar item se houver slots usando
                .IsRequired(false)
                .HasConstraintName("FK_EquipmentSlots_Item");
        });
    }

    /// <summary>
    /// Seed inicial de dados
    /// Autor: MonoDevPro
    /// Data: 2025-10-05 21:16:27
    /// </summary>
    private void SeedData(ModelBuilder modelBuilder)
    {
        // Items básicos
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
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
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
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
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
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
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
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
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
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new ItemStats
            {
                Id = 2,
                ItemId = 3, // Leather Armor
                BonusConstitution = 3,
                BonusPhysicalDefense = 10,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new ItemStats
            {
                Id = 3,
                ItemId = 4, // Magic Staff
                BonusIntelligence = 8,
                BonusMagicAttack = 20,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                IsActive = true
            }
        };

        modelBuilder.Entity<ItemStats>().HasData(itemStats);
    }
}