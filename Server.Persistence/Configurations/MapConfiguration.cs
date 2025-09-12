using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Simulation.Core.Models;

namespace Server.Persistence.Configurations;

public class MapConfiguration : IEntityTypeConfiguration<MapModel>
{
    public void Configure(EntityTypeBuilder<MapModel> builder)
    {
        builder.HasKey(m => m.MapId);
        builder.Property(m => m.Name).IsRequired().HasMaxLength(150);
        
        // Comparer para TileType[]
        var comparerTileTypeArray = new ValueComparer<TileType[]?>(
            (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == c2,
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c == null ? null : c.ToArray()
        );

        // Comparer para byte[]
        var comparerByteArray = new ValueComparer<byte[]?>(
            (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == c2,
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c == null ? null : c.ToArray()
        );

        // Converte o array de TileType[] para string (ex.: "1,2,3")
        var tileConverter = new ValueConverter<TileType[]?, string>(
            v => v != null ? string.Join(",", v.Select(e => (byte)e)) : string.Empty,
            v => string.IsNullOrEmpty(v)
                ? Array.Empty<TileType>()
                : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => (TileType)byte.Parse(s))
                    .ToArray()
        );

        // Converte o array de byte[] para string (ex.: "10,20,30")
        var byteConverter = new ValueConverter<byte[]?, string>(
            v => v != null ? string.Join(",", v) : string.Empty,
            v => string.IsNullOrEmpty(v)
                ? Array.Empty<byte>()
                : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(byte.Parse)
                    .ToArray()
        );

        builder.Property(m => m.TilesRowMajor)
            .HasConversion(tileConverter)
            .Metadata.SetValueComparer(comparerTileTypeArray);

        builder.Property(m => m.CollisionRowMajor)
            .HasConversion(byteConverter)
            .Metadata.SetValueComparer(comparerByteArray);
    }

}