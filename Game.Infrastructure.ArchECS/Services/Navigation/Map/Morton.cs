using System.Numerics;
using System.Runtime.CompilerServices;

namespace Game.Infrastructure.ArchECS.Services.Navigation.Map;

/// <summary>
/// Utilitários Morton Code otimizados com AggressiveInlining.
/// </summary>
public static class Morton
{
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    private static ulong Part1By1(ulong x)
    {
        x &= 0x00000000FFFFFFFFUL;
        x = (x | (x << 16)) & 0x0000FFFF0000FFFFUL;
        x = (x | (x << 8))  & 0x00FF00FF00FF00FFUL;
        x = (x | (x << 4))  & 0x0F0F0F0F0F0F0F0FUL;
        x = (x | (x << 2))  & 0x3333333333333333UL;
        x = (x | (x << 1))  & 0x5555555555555555UL;
        return x;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Compact1By1(ulong x)
    {
        x &= 0x5555555555555555UL;
        x = (x | (x >> 1))  & 0x3333333333333333UL;
        x = (x | (x >> 2))  & 0x0F0F0F0F0F0F0F0FUL;
        x = (x | (x >> 4))  & 0x00FF00FF00FF00FFUL;
        x = (x | (x >> 8))  & 0x0000FFFF0000FFFFUL;
        x = (x | (x >> 16)) & 0x00000000FFFFFFFFUL;
        return x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Encode(int x, int y) 
        => Part1By1((ulong)(uint)x) | (Part1By1((ulong)(uint)y) << 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int X, int Y) Decode(ulong code) 
        => ((int)Compact1By1(code), (int)Compact1By1(code >> 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NextPow2(int v) 
        => (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(1, v));
}