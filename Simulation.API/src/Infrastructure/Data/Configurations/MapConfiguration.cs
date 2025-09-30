using GameWeb.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameWeb.Infrastructure.Data.Configurations;

public class MapConfiguration : IEntityTypeConfiguration<Map>
{
    public void Configure(EntityTypeBuilder<Map> builder)
    {
        builder.ToTable("Maps");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Width)
            .IsRequired();

        builder.Property(m => m.Height)
            .IsRequired();

        // BLOB columns for tile/collision arrays
        builder.Property(m => m.Tiles)
            .HasColumnType("BLOB")
            .IsRequired();

        builder.Property(m => m.Collision)
            .HasColumnType("BLOB")
            .IsRequired();

        builder.Property(m => m.UsePadded)
            .IsRequired();

        builder.Property(m => m.BorderBlocked)
            .IsRequired();

        // Index optional: se você consultar por Name/Id etc.
        builder.HasIndex(m => m.Name);
    }
}
