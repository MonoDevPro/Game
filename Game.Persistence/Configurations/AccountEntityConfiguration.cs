using Game.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Game.Persistence.Configurations;

public class AccountEntityConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> entity)
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
    }
}