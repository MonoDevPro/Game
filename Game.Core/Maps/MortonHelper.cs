namespace Game.Core.Maps;

public static class MortonHelper
{
    // --- Helpers (64-bit Morton for up to 32-bit x,y) ---
    // Interleave lower 32 bits of x so there is one 0 bit between each original bit.
    private static ulong Part1By1(ulong x)
    {
        // masks for 64-bit interleave technique
        x &= 0x00000000FFFFFFFFUL;
        x = (x | (x << 16)) & 0x0000FFFF0000FFFFUL;
        x = (x | (x << 8))  & 0x00FF00FF00FF00FFUL;
        x = (x | (x << 4))  & 0x0F0F0F0F0F0F0F0FUL;
        x = (x | (x << 2))  & 0x3333333333333333UL;
        x = (x | (x << 1))  & 0x5555555555555555UL;
        return x;
    }

    // compact every second bit to the right (reverse of Part1By1)
    private static ulong Compact1By1(ulong x)
    {
        x &= 0x5555555555555555UL;
        x = (x | (x >> 1)) & 0x3333333333333333UL;
        x = (x | (x >> 2)) & 0x0F0F0F0F0F0F0F0FUL;
        x = (x | (x >> 4)) & 0x00FF00FF00FF00FFUL;
        x = (x | (x >> 8)) & 0x0000FFFF0000FFFFUL;
        x = (x | (x >> 16)) & 0x00000000FFFFFFFFUL;
        return x;
    }

    /// <summary>
    /// Encode (x,y) into a 64-bit Morton code (Z-order). Accepts 0 <= x,y < 2^32.
    /// </summary>
    public static ulong Encode(uint x, uint y)
    {
        return Part1By1(x) | (Part1By1(y) << 1);
    }

    /// <summary>
    /// Decode a 64-bit Morton code into (x,y).
    /// </summary>
    public static void Decode(ulong code, out uint x, out uint y)
    {
        x = (uint)Compact1By1(code);
        y = (uint)Compact1By1(code >> 1);
    }

    // --- Utilities for arrays ---

    /// <summary>
    /// Next power of two >= v.
    /// </summary>
    public static int NextPow2(int v)
    {
        if (v <= 0) return 1;
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v++;
        return v;
    }

    /// <summary>
    /// Get a Morton-based index inside a padded square array of size p x p,
    /// where p = next power-of-two(max(width,height)).
    /// The returned index is in [0, p*p-1]. You must allocate array of length p*p.
    /// This is the simplest approach (fast lookup), but wastes memory for non-power-of-two sizes.
    /// </summary>
    public static ulong MortonIndexPadded(int x, int y, int width, int height)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            throw new ArgumentOutOfRangeException(nameof(x), "x,y must be inside map bounds");

        int p = NextPow2(Math.Max(width, height));
        // encode using x,y as unsigned
        return Encode((uint)x, (uint)y); // result < p*p because p is power of two of needed bits
    }

    /// <summary>
    /// Build a mapping from (x,y) to dense rank [0..width*height-1] following Z-order but without padding waste.
    /// Returns two arrays:
    /// - posToRank[pos] = rank
    /// - rankToPos[rank] = (x,y)
    /// where pos = y*width + x
    /// </summary>
    public static (int[] posToRank, (int x, int y)[] rankToPos) BuildMortonMapping(int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentException("width/height > 0");

        int n = width * height;
        var arr = new (ulong key, int pos)[n];
        int idx = 0;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            ulong key = Encode((uint)x, (uint)y);
            arr[idx++] = (key, y * width + x);
        }

        // sort by Morton key
        Array.Sort(arr, (a, b) => a.key.CompareTo(b.key));

        var posToRank = new int[n];
        var rankToPos = new (int x, int y)[n];
        for (int rank = 0; rank < n; rank++)
        {
            int pos = arr[rank].pos;
            posToRank[pos] = rank;
            int x = pos % width;
            int y = pos / width;
            rankToPos[rank] = (x, y);
        }

        return (posToRank, rankToPos);
    }

    // helper to pack/unpack if needed:
    public static int PackXY(int x, int y) => (x & 0xFFFF) | (y << 16);
    public static (int x, int y) UnpackXY(int packed) => (packed & 0xFFFF, packed >> 16);
}