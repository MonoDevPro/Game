using Game.Domain.Enums;

namespace Game.ECS.Extensions;

public static class DirectionExtensions
{
    public static (int x, int y) ToIntVector(this Direction d) => d switch
    {
        Direction.North => (0, 1),
        Direction.NorthEast => (1, 1),
        Direction.East => (1, 0),
        Direction.SouthEast => (1, -1),
        Direction.South => (0, -1),
        Direction.SouthWest => (-1, -1),
        Direction.West => (-1, 0),
        Direction.NorthWest => (-1, 1),
        _ => (0, 0)
    };

    public static int ToAngleDegrees(this Direction d) => d switch
    {
        Direction.North => 90,
        Direction.NorthEast => 45,
        Direction.East => 0,
        Direction.SouthEast => 315,
        Direction.South => 270,
        Direction.SouthWest => 225,
        Direction.West => 180,
        Direction.NorthWest => 135,
        _ => 0
    };

    public static Direction FromAngleDegrees(int angle)
    {
        // normaliza 0..360
        var a = (angle % 360 + 360) % 360;
        // cada "fatia" de 45° (centro nas direções)
        var index = (int)Math.Round(a / 45.0) % 8;
        return index switch
        {
            0 => Direction.East,
            1 => Direction.NorthEast,
            2 => Direction.North,
            3 => Direction.NorthWest,
            4 => Direction.West,
            5 => Direction.SouthWest,
            6 => Direction.South,
            7 => Direction.SouthEast,
            _ => Direction.None
        };
    }
}
