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
        
        // Dados Seed
        SeedAccounts(entity);
    }

    private void SeedAccounts(EntityTypeBuilder<Account> entity)
    {
        var accounts = new[]
        {
            new Account
            {
                Id = 1,
                Username = "Admin",
                Email = "Admin@gmail.com",
                PasswordHash = "$2a$12$cyjTJ6fplEzKUkyTdgOUqeVYm.WZ1KoEm37jJiHY2oVz4hpB2cQ3W",
                PasswordSalt = [0x23, 0x0A, 0xC7, 0xC5, 0x75, 0x98, 0xA6, 0x7A, 0x8F, 0x24, 0x2A, 0x07, 0x0A, 
                    0xAF, 0x91, 0xC8, 0x5A, 0x8C, 0xEB, 0x77, 0x5E, 0xFF, 0x51, 0x7B, 0x3E, 0x20, 0x96, 0x90, 
                    0x6F, 0xB7, 0xB7, 0x51],
                IsEmailVerified = true,
                IsActive = true
            }
        };
        
        entity.HasData(accounts);
    }
}