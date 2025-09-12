using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulation.Core.Models;

namespace Server.Persistence.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<PlayerModel>
{
    public void Configure(EntityTypeBuilder<PlayerModel> builder)
    {
        // --- Configuração da Entidade PlayerTemplate ---
        builder.HasKey(p => p.Id); // Define a chave primária
        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);

        // Mapeia enums para serem armazenados como strings no banco de dados (mais legível)
        builder.Property(p => p.Gender).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Vocation).HasConversion<string>().HasMaxLength(20);
    }
}