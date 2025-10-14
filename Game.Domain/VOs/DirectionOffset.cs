using System.Runtime.InteropServices;

namespace Game.Domain.VOs;

/// <summary>
/// Representa um offset relativo em grid (signed byte: -128 a 127).
/// Usado para: input de movimento, direções relativas, ponteiro relativo.
/// Autor: MonoDevPro
/// Data: 2025-10-11 19:12:11
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DirectionOffset(sbyte X, sbyte Y)
{
    public static readonly DirectionOffset Zero = new(0, 0);
    public static readonly DirectionOffset One = new(1, 1);
    
    public static readonly DirectionOffset North = new(0, -1);
    public static readonly DirectionOffset South = new(0, 1);
    public static readonly DirectionOffset East = new(1, 0);
    public static readonly DirectionOffset West = new(-1, 0);
    public static readonly DirectionOffset NorthEast = new(1, -1);
    public static readonly DirectionOffset NorthWest = new(-1, -1);
    public static readonly DirectionOffset SouthEast = new(1, 1);
    public static readonly DirectionOffset SouthWest = new(-1, 1);
    
    public DirectionOffset Signed() => new((sbyte)Math.Sign(X), (sbyte)Math.Sign(Y));
    public int ManhattanDistance() => Math.Abs(X) + Math.Abs(Y);
    
    public static DirectionOffset operator +(DirectionOffset a, DirectionOffset b) 
        => new((sbyte)(a.X + b.X), (sbyte)(a.Y + b.Y));
    public static DirectionOffset operator -(DirectionOffset a, DirectionOffset b) 
        => new((sbyte)(a.X - b.X), (sbyte)(a.Y - b.Y));
    public static DirectionOffset operator *(DirectionOffset a, int scalar)
        => new((sbyte)(a.X * scalar), (sbyte)(a.Y * scalar));
    
    
    public override string ToString() => $"({X}, {Y})";
}
