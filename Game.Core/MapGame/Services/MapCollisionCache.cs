using System;
using System.Runtime.CompilerServices;
using Game.ECS.Components;

namespace Game.Core.MapGame.Services;

/// <summary>
/// Cache de colisões do mapa para acesso ultra-rápido (bitset 1 bit por célula em row-major).
/// - Independente do armazenamento interno (Morton/padded) do GameMapService.
/// - Leve e sem alocações em hot path.
/// - Fornece consultas O(1) por célula e varreduras eficientes por área.
/// </summary>
public sealed class MapCollisionCache
{
    public int Width { get; }
    public int Height { get; }
    public int Count => Width * Height;

    // 1 bit por célula (row-major). index = y*Width + x
    private readonly ulong[] _bits;

    private MapCollisionCache(int width, int height, ulong[] bits)
    {
        Width = width;
        Height = height;
        _bits = bits;
    }

    /// <summary>
    /// Constrói o cache a partir do mapa, convertendo CollisionMask para bitset row-major.
    /// </summary>
    public static MapCollisionCache Build(GameMapService map)
    {
        var width = map.Width;
        var height = map.Height;
        int n = checked(width * height);
        int wordCount = (n + 63) >> 6;

        var bits = new ulong[wordCount];

        int i = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++, i++)
            {
                int storageIdx = map.StorageIndex(x, y);
                if (map.CollisionMask[storageIdx] != 0)
                {
                    SetBit(bits, i);
                }
            }
        }

        return new MapCollisionCache(width, height, bits);
    }

    /// <summary>
    /// Reaplica todo o estado de colisão do mapa para este cache (full rebuild).
    /// </summary>
    public void ApplyFromMap(GameMapService map)
    {
        if (map.Width != Width || map.Height != Height)
            throw new ArgumentException("Map dimensions mismatch.");

        Array.Clear(_bits, 0, _bits.Length);

        int i = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++, i++)
            {
                int storageIdx = map.StorageIndex(x, y);
                if (map.CollisionMask[storageIdx] != 0)
                {
                    SetBit(_bits, i);
                }
            }
        }
    }

    /// <summary>
    /// Aplica um delta pontual vindo do mapa para o cache.
    /// </summary>
    public void ApplyCell(GameMapService map, in Position p)
    {
        if (!InBounds(p)) return;
        int idx = p.Y * Width + p.X;
        int storageIdx = map.StorageIndex(p.X, p.Y);
        bool blocked = map.CollisionMask[storageIdx] != 0;
        if (blocked) SetBit(_bits, idx);
        else ClearBit(_bits, idx);
    }

    /// <summary>
    /// Seta diretamente no cache o estado de bloqueio da célula.
    /// Não altera o GameMapService (use SetBlockedWithCache para manter ambos).
    /// </summary>
    public void SetBlocked(in Position p, bool blocked)
    {
        if (!InBounds(p)) return;
        int idx = p.Y * Width + p.X;
        if (blocked) SetBit(_bits, idx);
        else ClearBit(_bits, idx);
    }

    /// <summary>
    /// Consulta rápida (com bounds-check) do estado de bloqueio.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryIsBlocked(in Position p, out bool blocked)
    {
        if (!InBounds(p)) { blocked = true; return false; }
        int idx = p.Y * Width + p.X;
        blocked = TestBit(_bits, idx);
        return true;
    }

    /// <summary>
    /// Consulta sem bounds-check (hot path). Certifique-se de validar bounds antes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBlockedUnchecked(int x, int y)
    {
        int idx = y * Width + x;
        return TestBit(_bits, idx);
    }

    /// <summary>
    /// Retorna true se qualquer célula bloqueada existir na área (inclusiva).
    /// </summary>
    public bool AnyBlockedInArea(int minX, int minY, int maxX, int maxY)
    {
        if (minX < 0) minX = 0;
        if (minY < 0) minY = 0;
        if (maxX >= Width) maxX = Width - 1;
        if (maxY >= Height) maxY = Height - 1;
        if (maxX < minX || maxY < minY) return false;

        int rowStart = minY * Width;
        int rowEnd = maxY * Width;

        for (int y = minY, baseIdx = rowStart; y <= maxY; y++, baseIdx += Width)
        {
            int start = baseIdx + minX;
            int end = baseIdx + maxX;

            int startWord = start >> 6;
            int endWord = end >> 6;

            // Primeiro word (parcial)
            ulong word = _bits[startWord];
            int startBit = start & 63;
            int endBit = (startWord == endWord) ? (end & 63) : 63;
            ulong mask = MakeMask(startBit, endBit);
            if ((word & mask) != 0) return true;

            // Words completos entre eles
            for (int w = startWord + 1; w < endWord; w++)
            {
                if (_bits[w] != 0UL) return true;
            }

            // Último word (parcial)
            if (endWord != startWord)
            {
                word = _bits[endWord];
                startBit = 0;
                endBit = end & 63;
                mask = MakeMask(startBit, endBit);
                if ((word & mask) != 0) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Conta quantas células bloqueadas existem na área (inclusiva).
    /// </summary>
    public int CountBlockedInArea(int minX, int minY, int maxX, int maxY)
    {
        if (minX < 0) minX = 0;
        if (minY < 0) minY = 0;
        if (maxX >= Width) maxX = Width - 1;
        if (maxY >= Height) maxY = Height - 1;
        if (maxX < minX || maxY < minY) return 0;

        int total = 0;

        for (int y = minY; y <= maxY; y++)
        {
            int start = y * Width + minX;
            int end = y * Width + maxX;

            int startWord = start >> 6;
            int endWord = end >> 6;

            // Primeiro word (parcial)
            {
                ulong word = _bits[startWord];
                int startBit = start & 63;
                int endBit = (startWord == endWord) ? (end & 63) : 63;
                ulong mask = MakeMask(startBit, endBit);
                total += PopCount(word & mask);
            }

            // Words completos
            for (int w = startWord + 1; w < endWord; w++)
            {
                total += PopCount(_bits[w]);
            }

            // Último word (parcial)
            if (endWord != startWord)
            {
                ulong word = _bits[endWord];
                int endBit = end & 63;
                ulong mask = MakeMask(0, endBit);
                total += PopCount(word & mask);
            }
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool InBounds(in Position p) => (uint)p.X < (uint)Width && (uint)p.Y < (uint)Height;

    // -------- Bit helpers --------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetBit(ulong[] bits, int index)
    {
        int word = index >> 6;
        int bit = index & 63;
        bits[word] |= 1UL << bit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClearBit(ulong[] bits, int index)
    {
        int word = index >> 6;
        int bit = index & 63;
        bits[word] &= ~(1UL << bit);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TestBit(ulong[] bits, int index)
    {
        int word = index >> 6;
        int bit = index & 63;
        return (bits[word] & (1UL << bit)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MakeMask(int startBit, int endBit)
    {
        // Cria máscara com bits [startBit..endBit] ligados (0..63)
        int width = endBit - startBit + 1;
        ulong mask = width >= 64 ? ulong.MaxValue : ((1UL << width) - 1UL);
        return mask << startBit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int PopCount(ulong x)
    {
#if NET7_0_OR_GREATER
        return System.Numerics.BitOperations.PopCount(x);
#else
        // Fallback simples
        x = x - ((x >> 1) & 0x5555555555555555UL);
        x = (x & 0x3333333333333333UL) + ((x >> 2) & 0x3333333333333333UL);
        return (int)((((x + (x >> 4)) & 0x0F0F0F0F0F0F0F0FUL) * 0x0101010101010101UL) >> 56);
#endif
    }
}