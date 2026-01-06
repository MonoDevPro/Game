using Game.Domain.Entities;
using Game.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Game.Persistence.Configurations;

public class ItemEntityConfiguration : IEntityTypeConfiguration<Item>, IEntityTypeConfiguration<ItemStats>
{
    public void Configure(EntityTypeBuilder<Item> entity)
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
        
        // Dados Seed
        SeedItems(entity);
    }

    public void Configure(EntityTypeBuilder<ItemStats> entity)
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
        
        // Dados Seed
        SeedItemStats(entity);
    }

    private void SeedItems(EntityTypeBuilder<Item> entity)
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

        entity.HasData(items);
    }

    private void SeedItemStats(EntityTypeBuilder<ItemStats> statsEntity)
    {
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

        statsEntity.HasData(itemStats);
    }
}