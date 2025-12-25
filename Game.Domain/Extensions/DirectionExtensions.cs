using Game.Domain.Enums;
using Game.Domain.ValueObjects.Map;

namespace Game.Domain.Extensions;

/// <summary>
/// Extensões para MovementDirection. 
/// </summary>
public static class DirectionExtensions
{
    private static readonly int[] DeltaX = { 0, 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly int[] DeltaY = { 0, -1, -1, 0, 1, 1, 1, 0, -1 };

    public static (int DX, int DY) ToOffset(this DirectionType dir)
        => (DeltaX[(int)dir], DeltaY[(int)dir]);

    public static bool IsDiagonal(this DirectionType dir)
        => dir is DirectionType.NorthEast or DirectionType.SouthEast 
                or DirectionType.SouthWest or DirectionType.NorthWest;

    public static DirectionType FromOffset(int dx, int dy)
    {
        return (Math.Sign(dx), Math.Sign(dy)) switch
        {
            (0, -1) => DirectionType.North,
            (1, -1) => DirectionType.NorthEast,
            (1, 0) => DirectionType.East,
            (1, 1) => DirectionType.SouthEast,
            (0, 1) => DirectionType.South,
            (-1, 1) => DirectionType.SouthWest,
            (-1, 0) => DirectionType.West,
            (-1, -1) => DirectionType.NorthWest,
            _ => DirectionType.None
        };
    }

    public static DirectionType FromPositions(GridPosition from, GridPosition to)
        => FromOffset(to.X - from.X, to.Y - from.Y);

    public static DirectionType Opposite(this DirectionType dir)
    {
        return dir switch
        {
            DirectionType.North => DirectionType.South,
            DirectionType.NorthEast => DirectionType.SouthWest,
            DirectionType.East => DirectionType.West,
            DirectionType.SouthEast => DirectionType.NorthWest,
            DirectionType.South => DirectionType.North,
            DirectionType.SouthWest => DirectionType.NorthEast,
            DirectionType.West => DirectionType.East,
            DirectionType.NorthWest => DirectionType.SouthEast,
            _ => DirectionType.None
        };
    }
}