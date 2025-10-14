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
    public DbSet<Stats> Stats { get; set; }
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

        // Configurar entidades
        ConfigureAccount(modelBuilder);
        ConfigureCharacter(modelBuilder);
        ConfigureStats(modelBuilder);
        ConfigureItem(modelBuilder);
        ConfigureItemStats(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigureInventorySlot(modelBuilder);
        ConfigureEquipmentSlot(modelBuilder);
        ConfigureMap(modelBuilder);

        // Seed data
        SeedData(modelBuilder);
    }

    // ========== ENTITY CONFIGURATIONS ==========

    /// <summary>
    /// Configuração da entidade Account com cascade delete para Characters.
    /// </summary>
    private void ConfigureAccount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
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

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // ✅ Relacionamento: Account (1) -> Characters (N) com CASCADE DELETE
            // Deletar Account = Deletar todos os Characters (e seus relacionamentos em cascata)
            entity.HasMany(e => e.Characters)
                .WithOne(c => c.Account)
                .HasForeignKey(c => c.AccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Characters_Account");
        });
    }

    /// <summary>
    /// Configuração da entidade Character com cascade delete para Stats, Inventory e Equipment.
    /// </summary>
    private void ConfigureCharacter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasKey(e => e.Id);
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

            entity.Property(e => e.DirectionEnum)
                .HasConversion<string>()
                .HasMaxLength(10);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // ✅ Relacionamento: Character (1) -> Stats (1) com CASCADE DELETE
            // Deletar Character = Deletar Stats automaticamente
            entity.HasOne(e => e.Stats)
                .WithOne(s => s.Character)
                .HasForeignKey<Stats>(s => s.CharacterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Stats_Character");

            // ✅ Relacionamento: Character (1) -> Inventory (1) com CASCADE DELETE
            // Deletar Character = Deletar Inventory (e seus Slots via outra cascata)
            entity.HasOne(e => e.Inventory)
                .WithOne(i => i.Character)
                .HasForeignKey<Inventory>(i => i.CharacterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Inventory_Character");

            // ✅ Relacionamento: Character (1) -> EquipmentSlots (N) com CASCADE DELETE
            // Deletar Character = Deletar todos os EquipmentSlots
            entity.HasMany(e => e.Equipment)
                .WithOne(eq => eq.Character)
                .HasForeignKey(eq => eq.CharacterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_EquipmentSlots_Character");
        });
    }

    /// <summary>
    /// Configuração da entidade Stats (relacionamento 1:1 com Character).
    /// </summary>
    private void ConfigureStats(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Stats>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Stats");

            // Índices
            entity.HasIndex(e => e.CharacterId)
                .IsUnique()
                .HasDatabaseName("IX_Stats_CharacterId");

            entity.HasIndex(e => e.Level)
                .HasDatabaseName("IX_Stats_Level");

            // Propriedades com valores padrão
            entity.Property(e => e.Level).HasDefaultValue(1);
            entity.Property(e => e.Experience).HasDefaultValue(0);
            entity.Property(e => e.BaseStrength).HasDefaultValue(5);
            entity.Property(e => e.BaseDexterity).HasDefaultValue(5);
            entity.Property(e => e.BaseIntelligence).HasDefaultValue(5);
            entity.Property(e => e.BaseConstitution).HasDefaultValue(5);
            entity.Property(e => e.BaseSpirit).HasDefaultValue(5);
            entity.Property(e => e.CurrentHp).HasDefaultValue(50);
            entity.Property(e => e.CurrentMp).HasDefaultValue(30);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            // Ignorar propriedades calculadas (NotMapped)
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
    /// Configuração da entidade Item.
    /// </summary>
    private void ConfigureItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id);
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

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // ✅ Relacionamento: Item (1) -> ItemStats (0..1) com CASCADE DELETE
            // Deletar Item = Deletar ItemStats automaticamente
            entity.HasOne(e => e.Stats)
                .WithOne(s => s.Item)
                .HasForeignKey<ItemStats>(s => s.ItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false)
                .HasConstraintName("FK_ItemStats_Item");
        });
    }

    /// <summary>
    /// Configuração da entidade ItemStats (relacionamento 1:1 com Item).
    /// </summary>
    private void ConfigureItemStats(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ItemStats>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ItemStats");

            // Índices
            entity.HasIndex(e => e.ItemId)
                .IsUnique()
                .HasDatabaseName("IX_ItemStats_ItemId");

            // Propriedades com valores padrão 0
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
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });
    }

    /// <summary>
    /// Configuração da entidade Inventory com cascade delete para InventorySlots.
    /// </summary>
    private void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Inventories");

            // Índices
            entity.HasIndex(e => e.CharacterId)
                .IsUnique()
                .HasDatabaseName("IX_Inventories_CharacterId");

            // Propriedades
            entity.Property(e => e.Capacity)
                .HasDefaultValue(30);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // ✅ Relacionamento: Inventory (1) -> InventorySlots (N) com CASCADE DELETE
            // Deletar Inventory = Deletar todos os InventorySlots automaticamente
            entity.HasMany(e => e.Slots)
                .WithOne(s => s.Inventory)
                .HasForeignKey(s => s.InventoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_InventorySlots_Inventory");
        });
    }

    /// <summary>
    /// Configuração da entidade InventorySlot.
    /// ⚠️ DELETE RESTRICT para Item: Não deletar Item se houver slots usando.
    /// </summary>
    private void ConfigureInventorySlot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventorySlot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("InventorySlots");

            // Índices
            entity.HasIndex(e => new { e.InventoryId, e.SlotIndex })
                .IsUnique()
                .HasDatabaseName("IX_InventorySlots_InventoryId_SlotIndex");

            entity.HasIndex(e => e.ItemId)
                .HasDatabaseName("IX_InventorySlots_ItemId");

            // Propriedades
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // ⚠️ Relacionamento: InventorySlot (N) -> Item (0..1) com RESTRICT
            // Não permitir deletar Item se houver slots usando (proteção de integridade)
            entity.HasOne(e => e.Item)
                .WithMany()
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false)
                .HasConstraintName("FK_InventorySlots_Item");
        });
    }

    /// <summary>
    /// Configuração da entidade EquipmentSlot.
    /// ⚠️ DELETE RESTRICT para Item: Não deletar Item se houver equipamentos usando.
    /// </summary>
    private void ConfigureEquipmentSlot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EquipmentSlot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("EquipmentSlots");

            // Índices
            entity.HasIndex(e => new { e.CharacterId, e.SlotType })
                .IsUnique()
                .HasDatabaseName("IX_EquipmentSlots_CharacterId_SlotType");

            entity.HasIndex(e => e.ItemId)
                .HasDatabaseName("IX_EquipmentSlots_ItemId");

            // Propriedades
            entity.Property(e => e.SlotType)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            // ⚠️ Relacionamento: EquipmentSlot (N) -> Item (0..1) com RESTRICT
            // Não permitir deletar Item se houver equipamentos usando (proteção de integridade)
            entity.HasOne(e => e.Item)
                .WithMany()
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false)
                .HasConstraintName("FK_EquipmentSlots_Item");
        });
    }

    /// <summary>
    /// Configuração da entidade Map.
    /// </summary>
    private void ConfigureMap(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Map>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Maps");

            // Índices
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_Maps_Name");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Maps_IsActive");

            // Propriedades
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Width)
                .IsRequired();

            entity.Property(e => e.Height)
                .IsRequired();

            entity.Property(e => e.UsePadded)
                .HasDefaultValue(false);

            entity.Property(e => e.BorderBlocked)
                .HasDefaultValue(true);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });
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