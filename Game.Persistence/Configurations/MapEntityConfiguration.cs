using Game.Domain.Entities;
using Game.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Game.Persistence.Configurations;

public class MapEntityConfiguration : IEntityTypeConfiguration<Map>
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

        builder.Property(m => m.Layers)
            .IsRequired();

        builder.Property(m => m.BorderBlocked)
            .IsRequired();
        
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        // Serializar Tiles como bytes (mais eficiente)
        // Converter Tiles para bytes
        var converter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Tile[], byte[]>(
            tiles => SerializeTiles(tiles),
            bytes => DeserializeTiles(bytes)
        );
        
        var comparer = new ValueComparer<Tile[]>(
            equalsExpression: (a, b) => a!.SequenceEqual(b!),
            hashCodeExpression: a => a.Aggregate(0, (acc, tile) => HashCode.Combine(acc, tile.Type, tile.CollisionMask)),
            snapshotExpression: a => a.ToArray()
        );
        
        builder.Property(m => m.Tiles)
            .HasConversion(converter)
            .Metadata.SetValueComparer(comparer);

        builder.Property(m => m.Tiles)
            .HasColumnType("BLOB")
            .IsRequired();
        
        // Propriedade calculada - ignorar
        builder.Ignore(m => m.Count);

        // Índices
        builder.HasIndex(m => m.Name);
        builder.HasIndex(m => new { m.Width, m.Height });
        
        // Seed inicial de mapas
        SeedMaps(builder);
    }
    
    private void SeedMaps(EntityTypeBuilder<Map> entity)
    {
        var maps = new[]
        {
            new Map(name: "Starter Village", width: 100, height: 100, layers: 1, borderBlocked: true)
            {
                Id = 1,
                IsActive = true
            },
            new Map(name: "Forest of Beginnings", width: 200, height: 200, layers: 1, borderBlocked: true)
            {
                Id = 2,
                IsActive = true
            }
        };
        
        // Inicializar tiles padrão
        foreach (var map in maps)
        {
            for (int i = 0; i < map.Tiles.Length; i++)
            {
                map.Tiles[i] = new Tile
                {
                    Type = TileType.Floor,
                    CollisionMask = 0
                };
            }
        }

        entity.HasData(maps);
    }
    
    private static byte[] SerializeTiles(Tile[] tiles)
    {
        if (tiles.Length == 0)
            return [];

        var bytes = new byte[tiles.Length * 2];
        
        for (int i = 0; i < tiles.Length; i++)
        {
            bytes[i * 2] = (byte)tiles[i].Type;
            bytes[i * 2 + 1] = tiles[i].CollisionMask;
        }

        return bytes;
    }

    private static Tile[] DeserializeTiles(byte[] bytes)
    {
        if (bytes.Length == 0)
            return [];

        var tiles = new Tile[bytes.Length / 2];
        
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = new Tile
            {
                Type = (TileType)bytes[i * 2],
                CollisionMask = bytes[i * 2 + 1]
            };
        }

        return tiles;
    }
}