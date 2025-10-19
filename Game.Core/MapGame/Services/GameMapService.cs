using Game.Core.MapGame.Utils;
using Game.Domain.Enums;
using Game.ECS.Components;

namespace Game.Core.MapGame.Services;

public sealed class GameMapService
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
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

    public GameMapService(int id, string name, int width, int height, bool usePadded, bool borderBlocked = true)
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
                coll[StorageIndexUnchecked(x, 0)] = 1;
                coll[StorageIndexUnchecked(x, h - 1)] = 1;
            }
            for (int y = 0; y < h; y++)
            {
                coll[StorageIndexUnchecked(0, y)] = 1;
                coll[StorageIndexUnchecked(w - 1, y)] = 1;
            }
        }
    }

    // helpers
    private int LinearPosUnchecked(int x, int y) => y * Width + x;
    private int LinearPos(in Position p) => p.Y * Width + p.X;

    public bool InBoundsFast(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;
    public bool InBounds(in Position p) => (uint)p.X < (uint)Width && (uint)p.Y < (uint)Height;

    public Position ClampToBounds(in Position p)
    {
        int x = p.X < 0 ? 0 : (p.X >= Width ? Width - 1 : p.X);
        int y = p.Y < 0 ? 0 : (p.Y >= Height ? Height - 1 : p.Y);
        return new Position { X = x, Y = y };
    }

    // StorageIndex: versões sem-throw (hot path) e checadas (debug/fallback)
    public bool TryStorageIndex(int x, int y, out int idx)
    {
        if (!InBoundsFast(x, y)) { idx = -1; return false; }
        if (UsePadded)
        {
            idx = (int)MortonHelper.MortonIndexPadded(x, y, Width, Height);
            return true;
        }
        int pos = LinearPosUnchecked(x, y);
        idx = _posToRank![pos];
        return true;
    }

    public bool TryStorageIndex(in Position p, out int idx) => TryStorageIndex(p.X, p.Y, out idx);

    // Versão unchecked para uso interno onde bounds já foram validados
    private int StorageIndexUnchecked(int x, int y)
    {
        if (UsePadded)
            return (int)MortonHelper.MortonIndexPadded(x, y, Width, Height);
        int pos = LinearPosUnchecked(x, y);
        return _posToRank![pos];
    }

    public int StorageIndex(int x, int y)
    {
        if (!InBoundsFast(x, y)) throw new ArgumentOutOfRangeException();
        return StorageIndexUnchecked(x, y);
    }

    public int StorageIndex(in Position p) => StorageIndex(p.X, p.Y);

    // accessors sem-throw (hot path)
    public bool TryGetTile(in Position p, out TileType tile)
    {
        if (!TryStorageIndex(p, out int idx)) { tile = default; return false; }
        tile = Tiles[idx];
        return true;
    }

    public bool TrySetTile(in Position p, TileType t)
    {
        if (!TryStorageIndex(p, out int idx)) return false;
        Tiles[idx] = t;
        return true;
    }

    public bool TryIsBlocked(in Position p, out bool blocked)
    {
        if (!TryStorageIndex(p, out int idx)) { blocked = true; return false; }
        blocked = CollisionMask[idx] != 0;
        return true;
    }

    public bool TrySetBlocked(in Position p, bool blocked)
    {
        if (!TryStorageIndex(p, out int idx)) return false;
        CollisionMask[idx] = blocked ? (byte)1 : (byte)0;
        return true;
    }

    // APIs compatíveis (com throw) para uso fora do hot path
    public TileType GetTile(in Position p)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        return Tiles[StorageIndex(p)];
    }

    public void SetTile(in Position p, TileType t)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        Tiles[StorageIndex(p)] = t;
    }

    public bool IsBlocked(in Position p)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        return CollisionMask[StorageIndex(p)] != 0;
    }

    public void SetBlocked(in Position p, bool blocked)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        CollisionMask[StorageIndex(p)] = blocked ? (byte)1 : (byte)0;
    }

    // Optional: get coordinates from storage index (only meaningful for compact mode)
    public (int x, int y) CoordFromRank(int rank)
    {
        if (UsePadded) throw new InvalidOperationException("CoordFromRank is only valid for compact mode");
        if (rank < 0 || rank >= Count) throw new ArgumentOutOfRangeException(nameof(rank));
        return _rankToPos![rank];
    }

    // helper: fill from row-major input arrays (useful when loading map data)
    // Input arrays are assumed row-major size width*height.
    public void PopulateFromRowMajor(TileType[] tilesRowMajor, byte[]? collisionRowMajor)
    {
        if (tilesRowMajor == null) throw new ArgumentNullException(nameof(tilesRowMajor));
        if (tilesRowMajor.Length != Width * Height) throw new ArgumentException("tiles length mismatch");
        if (collisionRowMajor != null && collisionRowMajor.Length != Width * Height) throw new ArgumentException("collision length mismatch");

        if (UsePadded)
        {
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
            for (int pos = 0; pos < Width * Height; pos++)
            {
                int r = _posToRank![pos];
                Tiles[r] = tilesRowMajor[pos];
                if (collisionRowMajor != null) CollisionMask[r] = collisionRowMajor[pos];
            }
        }

        // Reforça bordas bloqueadas após popular, se habilitado
        if (BorderBlocked)
        {
            for (int x = 0; x < Width; x++)
            {
                CollisionMask[StorageIndexUnchecked(x, 0)] = 1;
                CollisionMask[StorageIndexUnchecked(x, Height - 1)] = 1;
            }
            for (int y = 0; y < Height; y++)
            {
                CollisionMask[StorageIndexUnchecked(0, y)] = 1;
                CollisionMask[StorageIndexUnchecked(Width - 1, y)] = 1;
            }
        }
    }

    // Vizinhança sem alocação (visitor com early-exit)
    public void ForEachNeighbors4(in Position p, Func<int, int, bool> visitor)
    {
        // esquerda
        if (p.X > 0 && !visitor(p.X - 1, p.Y)) return;
        // direita
        if (p.X + 1 < Width && !visitor(p.X + 1, p.Y)) return;
        // cima
        if (p.Y > 0 && !visitor(p.X, p.Y - 1)) return;
        // baixo
        if (p.Y + 1 < Height) visitor(p.X, p.Y + 1);
    }

    // Query de walkables escrevendo em Span (sem GC). Retorna quantos preenchidos.
    public int QueryWalkableArea(in Position minInclusive, in Position maxInclusive, Span<Position> results)
    {
        int write = 0;
        int minX = Math.Max(0, minInclusive.X);
        int minY = Math.Max(0, minInclusive.Y);
        int maxX = Math.Min(Width - 1, maxInclusive.X);
        int maxY = Math.Min(Height - 1, maxInclusive.Y);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (write >= results.Length) return write;
                int idx = StorageIndexUnchecked(x, y);
                if (CollisionMask[idx] == 0)
                {
                    results[write++] = new Position { X = x, Y = y };
                }
            }
        }
        return write;
    }

    public long CountBlockedCells(bool parallel = false)
    {
        var arr = CollisionMask;
        if (arr.Length == 0) return 0L;

        if (!parallel)
        {
            int cnt = 0;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] != 0) cnt++;
            return cnt;
        }
        long total = 0;
        Parallel.For(0, arr.Length,
            () => 0L,
            (i, state, local) => arr[i] != 0 ? local + 1 : local,
            local => Interlocked.Add(ref total, local)
        );
        return total;
    }
}