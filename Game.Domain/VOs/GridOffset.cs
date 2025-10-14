using System.Runtime.InteropServices;

namespace Game.Domain.VOs;

/// <summary>
/// Representa um offset relativo em grid (signed byte: -128 a 127).
/// Usado para: input de movimento, direções relativas, ponteiro relativo.
/// Autor: MonoDevPro
/// Data: 2025-10-11 19:12:11
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct GridOffset(sbyte X, sbyte Y)
{
    public static readonly GridOffset Zero = new(0, 0);
    public static readonly GridOffset One = new(1, 1);
    
    public static readonly GridOffset North = new(0, -1);
    public static readonly GridOffset South = new(0, 1);
    public static readonly GridOffset East = new(1, 0);
    public static readonly GridOffset West = new(-1, 0);
    public static readonly GridOffset NorthEast = new(1, -1);
    public static readonly GridOffset NorthWest = new(-1, -1);
    public static readonly GridOffset SouthEast = new(1, 1);
    public static readonly GridOffset SouthWest = new(-1, 1);
    
    public GridOffset Signed() => new((sbyte)Math.Sign(X), (sbyte)Math.Sign(Y));
    public int ManhattanDistance() => Math.Abs(X) + Math.Abs(Y);
    
    public static GridOffset operator +(GridOffset a, GridOffset b) 
        => new((sbyte)(a.X + b.X), (sbyte)(a.Y + b.Y));
    public static GridOffset operator -(GridOffset a, GridOffset b) 
        => new((sbyte)(a.X - b.X), (sbyte)(a.Y - b.Y));
    public static GridOffset operator *(GridOffset a, int scalar)
        => new((sbyte)(a.X * scalar), (sbyte)(a.Y * scalar));
    
    
    public override string ToString() => $"({X}, {Y})";
}
