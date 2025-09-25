using Simulation.Core.ECS.Components;

namespace Simulation.Core.ECS.Services;

public class MapService
{
    public int Id { get; init; }
    public string Name { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int Count => Width * Height;

    // storage mode
    public bool UsePadded { get; init; } = false;
    public int PaddedSize { get; init; } = 0; // p (if padded)
    
    public bool BorderBlocked { get; init; }

    // mapping (used for compact mode)
    // posToRank: index by linear pos (y*width + x) -> rank (0..Count-1)
    private readonly int[]? _posToRank;
    private readonly (int x, int y)[]? _rankToPos;

    // storage arrays: ALWAYS kept in Morton order (rank order) or padded order
    public TileType[] Tiles { get; private set; }
    public byte[] CollisionMask { get; private set; } // 0=free, 1=blocked

    private MapService(int id, string name, int width, int height, bool usePadded, bool borderBlocked = true)
    {
        Id = id;
        Name = name ?? string.Empty;
        Width = width;
        Height = height;
        UsePadded = usePadded;
        BorderBlocked = borderBlocked;

        if (usePadded)
        {
            int p = MortonHelper.NextPow2(Math.Max(width, height));
            PaddedSize = p;
            long pCount = (long)p * (long)p;
            if (pCount > int.MaxValue) throw new ArgumentException("padded size too large");
            Tiles = new TileType[p * p];
            CollisionMask = new byte[p * p];
            _posToRank = null;
            _rankToPos = null;
        }
        else
        {
            (int[] posToRank, (int x, int y)[] rankToPos) = MortonHelper.BuildMortonMapping(width, height);
            _posToRank = posToRank;
            _rankToPos = rankToPos;
            Tiles = new TileType[width * height];
            CollisionMask = new byte[width * height];
        }
        
        // optionally block borders
        if (borderBlocked)
        {
            var coll = CollisionMask;
            int w = Width;
            int h = Height;
            for (int x = 0; x < w; x++)
            {
                coll[x] = 1;
                coll[(h - 1) * w + x] = 1;
            }
            for (int y = 0; y < h; y++)
            {
                coll[y * w] = 1;
                coll[y * w + (w - 1)] = 1;
            }
        }
    }

    // Factory
    public static MapService CreateFromTemplate(MapData data)
    {
        int w = data.Width;
        int h = data.Height;

        if (w <= 0 || h <= 0)
            throw new ArgumentException($"Invalid map dimensions in template Id={data.Id}: width={w}, height={h}");

        var expected = w * h;

        // defensivo: nunca trabalhar com null
        var tiles = data.TilesRowMajor ?? [];
        if (tiles.Length != expected)
        {
            var fallbackTiles = new TileType[expected];
            Array.Fill(fallbackTiles, TileType.Floor);
            tiles = fallbackTiles;
        }

        var collision = data.CollisionRowMajor ?? [];
        if (collision.Length != expected)
        {
            collision = new byte[expected]; // default = 0 -> no collision
        }

        // cria a instância e popula os arrays internos
        var ms = new MapService(data.Id, data.Name ?? string.Empty, w, h, data.UsePadded, data.BorderBlocked);

        // copia os dados reais para o armazenamento interno (considera padded/compact)
        ms.PopulateFromRowMajor(tiles, collision);

        // reaplica border blocking depois de popular, garantindo consistência
        if (ms.BorderBlocked)
        {
            var coll = ms.CollisionMask;
            int width = ms.Width;
            int height = ms.Height;
            // top/bottom
            for (int x = 0; x < width; x++)
            {
                coll[ms.StorageIndex(x, 0)] = 1;
                coll[ms.StorageIndex(x, height - 1)] = 1;
            }
            // left/right
            for (int y = 0; y < height; y++)
            {
                coll[ms.StorageIndex(0, y)] = 1;
                coll[ms.StorageIndex(width - 1, y)] = 1;
            }
        }

        return ms;
    }


    // helpers
    private int LinearPos(int x, int y) => y * Width + x;
    private int LinearPos(Position p) => p.Y * Width + p.X;

    // converts (x,y) -> storage index (rank or padded idx)
    public int StorageIndex(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) throw new ArgumentOutOfRangeException();
        if (UsePadded)
        {
            // padded: Morton code fits in padded square
            ulong idx = MortonHelper.MortonIndexPadded(x, y, Width, Height);
            return (int)idx;
        }
        else
        {
            int pos = LinearPos(x, y);
            return _posToRank![pos];
        }
    }

    public int StorageIndex(Position p)
    {
        return StorageIndex(p.X, p.Y);
    }

    // accessors
    public TileType GetTile(Position p)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        return Tiles[StorageIndex(p)];
    }

    public void SetTile(Position p, TileType t)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        Tiles[StorageIndex(p)] = t;
    }

    public bool IsBlocked(Position p)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        return CollisionMask[StorageIndex(p)] != 0;
    }

    public void SetBlocked(Position p, bool blocked)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        CollisionMask[StorageIndex(p)] = blocked ? (byte)1 : (byte)0;
    }

    public bool InBounds(Position p)
        => (uint)p.X < (uint)Width && (uint)p.Y < (uint)Height;

    // Optional: get coordinates from storage index (only meaningful for compact mode)
    public (int x, int y) CoordFromRank(int rank)
    {
        if (UsePadded) throw new InvalidOperationException("CoordFromRank is only valid for compact mode");
        if (rank < 0 || rank >= Count) throw new ArgumentOutOfRangeException(nameof(rank));
        return _rankToPos![rank];
    }

    // helper: fill from row-major input arrays (useful when loading map data)
    // Input arrays are assumed row-major size width*height.
    public void PopulateFromRowMajor(TileType[] tilesRowMajor, byte[] collisionRowMajor)
    {
        if (tilesRowMajor == null) throw new ArgumentNullException(nameof(tilesRowMajor));
        if (tilesRowMajor.Length != Width * Height) throw new ArgumentException("tiles length mismatch");
        if (collisionRowMajor != null && collisionRowMajor.Length != Width * Height) throw new ArgumentException("collision length mismatch");

        if (UsePadded)
        {
            // copy each (x,y) to padded storage using Morton index
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int srcPos = y * Width + x;
                    int dst = (int)MortonHelper.MortonIndexPadded(x, y, Width, Height);
                    Tiles[dst] = tilesRowMajor[srcPos];
                    if (collisionRowMajor != null) CollisionMask[dst] = collisionRowMajor[srcPos];
                }
            }
        }
        else
        {
            // compact: use pos->rank mapping
            for (int pos = 0; pos < Width * Height; pos++)
            {
                int r = _posToRank![pos];
                Tiles[r] = tilesRowMajor[pos];
                if (collisionRowMajor != null) CollisionMask[r] = collisionRowMajor[pos];
            }
        }
    }

    // small convenience: iterate neighbors in Manhattan 4-neighborhood
    public IEnumerable<(int x, int y)> Neighbors4(int x, int y)
    {
        if (x > 0) yield return (x - 1, y);
        if (x + 1 < Width) yield return (x + 1, y);
        if (y > 0) yield return (x, y - 1);
        if (y + 1 < Height) yield return (x, y + 1);
    }
    
    public long CountBlockedCells(bool parallel = false)
    {
        var arr = CollisionMask;
        if (arr.Length == 0) return 0L;

        if (!parallel)
        {
            int cnt = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] != 0) cnt++;
            }
            return cnt;
        }
        // Versão paralela que soma em um long de forma segura
        long total = 0;
        Parallel.For(0, arr.Length,
            () => 0L,
            (i, state, local) =>
            {
                if (arr[i] != 0) local++;
                return local;
            },
            local => Interlocked.Add(ref total, local)
        );
        return total;
    }
}