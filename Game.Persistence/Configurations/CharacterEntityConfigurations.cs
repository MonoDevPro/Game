using Game.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Game.Persistence.Configurations;

public class CharacterEntityConfiguration : 
    IEntityTypeConfiguration<Character>, 
    IEntityTypeConfiguration<Inventory>,
    IEntityTypeConfiguration<EquipmentSlot>,
    IEntityTypeConfiguration<InventorySlot>
{
    public void Configure(EntityTypeBuilder<Character> entity)
    {
        entity.HasKey(e => e.Id);
        entity.ToTable("Characters");

        // Índices
        entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("IX_Characters_Name");
        entity.HasIndex(e => e.AccountId).HasDatabaseName("IX_Characters_AccountId");
        entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Characters_IsActive");
        entity.HasIndex(e => new { Floor = e.PosZ, PositionX = e.PosX, PositionY = e.PosY }).HasDatabaseName("IX_Characters_Position");
        entity.HasIndex(e => e.Level).HasDatabaseName("IX_Level");

        // Propriedades
        entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Gender).HasConversion<string>().HasMaxLength(20);
        entity.Property(e => e.Vocation).HasConversion<string>().HasMaxLength(20);
            
        entity.Property(e => e.MapId).HasDefaultValue(1);

        entity.Property(e => e.DirX).HasDefaultValue(0);
        entity.Property(e => e.DirY).HasDefaultValue(1);
            
        entity.Property(e => e.PosX).HasDefaultValue(0);
        entity.Property(e => e.PosY).HasDefaultValue(0);
        entity.Property(e => e.PosZ).HasDefaultValue(0);

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

        entity.Property(e => e.IsActive).HasDefaultValue(true);

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
    }

    public void Configure(EntityTypeBuilder<Inventory> entity)
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
    }

    public void Configure(EntityTypeBuilder<EquipmentSlot> entity)
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
    }

    public void Configure(EntityTypeBuilder<InventorySlot> entity)
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
    }
}