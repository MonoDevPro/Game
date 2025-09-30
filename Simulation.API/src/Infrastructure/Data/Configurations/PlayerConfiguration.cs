using GameWeb.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameWeb.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Player
/// </summary>
public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");
        
        builder.HasKey(c => c.Id);
        
        builder.HasQueryFilter(c => c.IsActive);
        
        builder.Property(c => c.Name)
            .HasMaxLength(20)
            .IsRequired();
        
        builder.HasIndex(c => c.Id, "IX_Character_Id").IsUnique();
        builder.HasIndex(c => c.Name, "IX_Character_Name").IsUnique();


        // Enums stored compactly as tinyints
        builder.Property(p => p.Gender)
            .HasConversion<byte>()
            .HasColumnType("INTEGER")
            .IsRequired();

        builder.Property(p => p.Vocation)
            .HasConversion<byte>()
            .HasColumnType("INTEGER")
            .IsRequired();

        // Stats
        builder.Property(p => p.HealthMax).IsRequired();
        builder.Property(p => p.HealthCurrent).IsRequired();
        builder.Property(p => p.AttackDamage).IsRequired();
        builder.Property(p => p.AttackRange).IsRequired();

        // floats in SQLite are REAL
        builder.Property(p => p.AttackCastTime).HasColumnType("REAL");
        builder.Property(p => p.AttackCooldown).HasColumnType("REAL");
        builder.Property(p => p.MoveSpeed).HasColumnType("REAL");

        // Position and direction
        builder.Property(p => p.PosX).IsRequired();
        builder.Property(p => p.PosY).IsRequired();
        builder.Property(p => p.DirX).IsRequired();
        builder.Property(p => p.DirY).IsRequired();

        // Indexes: query players by name; spatial queries by pos
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => new { p.PosX, p.PosY });

        // Auditing columns (from BaseAuditableEntity) should be configured in the base OnModelCreating
    }
}
