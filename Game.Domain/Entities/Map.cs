using Game.Domain.Enums;

namespace Game.Domain.Entities;

// Célula do mapa (sem coordenadas — a posição é dada por x,y,z no Map)
public record struct Tile
{
    public TileType Type { get; set; }
    public byte CollisionMask { get; set; } // 0=livre, 1=bloqueado
}

// Entidade de domínio com 3 dimensões (X,Y,Z)
public class Map : BaseEntity
{
    public string Name { get; init; } = null!;
    public int Width { get; init; }
    public int Height { get; init; }
    public int Layers { get; init; } // Z
    public int Count => checked(Width * Height * Layers);
    public bool BorderBlocked { get; init; }

    // Armazenamento base (row-major por XY, camadas contíguas em Z)
    // idx = ((z * Height) + y) * Width + x
    public Tile[] Tiles { get; private set; } = [];

    public Map() { }

    public Map(string name, int width, int height, int layers, bool borderBlocked = false)
    {
        if (width <= 0 || height <= 0 || layers <= 0)
            throw new ArgumentException($"Invalid dimensions: {width}x{height}x{layers}");

        Name = name ?? string.Empty;
        Width = width;
        Height = height;
        Layers = layers;
        BorderBlocked = borderBlocked;
        Tiles = new Tile[checked(width * height * layers)];
    }

    // Bounds e indexação
    public bool InBounds(int x, int y, int z)
        => (uint)x < (uint)Width && (uint)y < (uint)Height && (uint)z < (uint)Layers;

    public int IndexOf(int x, int y, int z)
    {
        if (!InBounds(x, y, z)) throw new ArgumentOutOfRangeException();
        return ((z * Height) + y) * Width + x;
    }

    public bool TryIndexOf(int x, int y, int z, out int index)
    {
        if (!InBounds(x, y, z)) { index = -1; return false; }
        index = ((z * Height) + y) * Width + x;
        return true;
    }

    // Acesso pontual
    public Tile GetTile(int x, int y, int z) => Tiles[IndexOf(x, y, z)];

    public bool TryGetTile(int x, int y, int z, out Tile tile)
    {
        if (!TryIndexOf(x, y, z, out var idx)) { tile = default; return false; }
        tile = Tiles[idx];
        return true;
    }

    public void SetTile(int x, int y, int z, in Tile tile) => Tiles[IndexOf(x, y, z)] = tile;

    public bool TrySetTile(int x, int y, int z, in Tile tile)
    {
        if (!TryIndexOf(x, y, z, out var idx)) return false;
        Tiles[idx] = tile;
        return true;
    }

    // Slices por camada (sem alocação)
    public Span<Tile> GetLayerSpan(int z)
    {
        if ((uint)z >= (uint)Layers) throw new ArgumentOutOfRangeException(nameof(z));
        int offset = z * (Width * Height);
        return Tiles.AsSpan(offset, Width * Height);
    }

    public ReadOnlySpan<Tile> GetLayerReadOnlySpan(int z)
    {
        if ((uint)z >= (uint)Layers) throw new ArgumentOutOfRangeException(nameof(z));
        int offset = z * (Width * Height);
        return Tiles.AsSpan(offset, Width * Height);
    }

    // Preenchimento utilitário no domínio
    public void FillAll(in Tile value) => Array.Fill(Tiles, value);

    public void FillLayer(int z, in Tile value)
    {
        var span = GetLayerSpan(z);
        span.Fill(value);
    }

    // Iteração conveniente por camada (sem alocar)
    public IEnumerable<(int x, int y, Tile t)> EnumerateLayer(int z)
    {
        if ((uint)z >= (uint)Layers) yield break;
        int offset = z * (Width * Height);
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int index = offset + y * Width + x;
                yield return (x, y, Tiles[index]);
            }
    }
}
