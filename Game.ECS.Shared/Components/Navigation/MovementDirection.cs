namespace Game.ECS.Shared.Components.Navigation;

/// <summary>
/// Direção de movimento em 8 direções.
/// Compartilhado entre cliente e servidor.
/// </summary>
public enum MovementDirection : byte
{
    None = 0,
    North = 1,      // Y-
    NorthEast = 2,
    East = 3,       // X+
    SouthEast = 4,
    South = 5,      // Y+
    SouthWest = 6,
    West = 7,       // X-
    NorthWest = 8
}

/// <summary>
/// Extensões para MovementDirection. 
/// </summary>
public static class MovementDirectionExtensions
{
    private static readonly int[] DeltaX = { 0, 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly int[] DeltaY = { 0, -1, -1, 0, 1, 1, 1, 0, -1 };

    public static (int DX, int DY) ToOffset(this MovementDirection dir)
        => (DeltaX[(int)dir], DeltaY[(int)dir]);

    public static bool IsDiagonal(this MovementDirection dir)
        => dir is MovementDirection.NorthEast or MovementDirection.SouthEast 
                or MovementDirection.SouthWest or MovementDirection.NorthWest;

    public static MovementDirection FromOffset(int dx, int dy)
    {
        return (Math.Sign(dx), Math.Sign(dy)) switch
        {
            (0, -1) => MovementDirection.North,
            (1, -1) => MovementDirection.NorthEast,
            (1, 0) => MovementDirection.East,
            (1, 1) => MovementDirection.SouthEast,
            (0, 1) => MovementDirection.South,
            (-1, 1) => MovementDirection.SouthWest,
            (-1, 0) => MovementDirection.West,
            (-1, -1) => MovementDirection.NorthWest,
            _ => MovementDirection.None
        };
    }

    public static MovementDirection FromPositions(GridPosition from, GridPosition to)
        => FromOffset(to.X - from.X, to.Y - from.Y);

    public static MovementDirection Opposite(this MovementDirection dir)
    {
        return dir switch
        {
            MovementDirection.North => MovementDirection.South,
            MovementDirection.NorthEast => MovementDirection.SouthWest,
            MovementDirection.East => MovementDirection.West,
            MovementDirection.SouthEast => MovementDirection.NorthWest,
            MovementDirection.South => MovementDirection.North,
            MovementDirection.SouthWest => MovementDirection.NorthEast,
            MovementDirection.West => MovementDirection.East,
            MovementDirection.NorthWest => MovementDirection.SouthEast,
            _ => MovementDirection.None
        };
    }
}