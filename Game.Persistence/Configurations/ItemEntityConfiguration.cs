using Game.Domain.Entities;
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
    }
}