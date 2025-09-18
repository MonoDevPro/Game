using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulation.Core.Persistence.Models;

namespace Server.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<AccountModel>
{
    public void Configure(EntityTypeBuilder<AccountModel> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Username)
            .HasConversion( v => v.ToLower(), v => v) // Armazena sempre em minúsculas
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(a => a.Username)
            .IsUnique();

        builder.Property(a => a.PasswordHash)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.LastLoginAt)
            .IsRequired(false); // pode ser null/0 se preferir - aqui fica obrigatório (non-null) por design
    }
}