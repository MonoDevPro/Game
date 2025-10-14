using Game.Domain.Enums;
using Game.Domain.VOs;

namespace Game.Abstractions;

public static class DirectionExtensions
{
    public static Coordinate ToCoordinate(this DirectionEnum d) => d switch
    {
        DirectionEnum.North => new Coordinate(0, -1),
        DirectionEnum.NorthEast => new Coordinate(1, -1),
        DirectionEnum.NorthWest => new Coordinate(-1, -1),
        DirectionEnum.South => new Coordinate(0, 1),
        DirectionEnum.SouthEast => new Coordinate(1, 1),
        DirectionEnum.SouthWest => new Coordinate(-1, 1),
        DirectionEnum.East => new Coordinate(1, 0),
        DirectionEnum.West => new Coordinate(-1, 0),
        _ => new Coordinate(0, 0)
    };
    
    public static int ToAngleDegrees(this DirectionEnum d) => d switch
    {
        DirectionEnum.North => 90,
        DirectionEnum.NorthEast => 45,
        DirectionEnum.East => 0,
        DirectionEnum.SouthEast => 315,
        DirectionEnum.South => 270,
        DirectionEnum.SouthWest => 225,
        DirectionEnum.West => 180,
        DirectionEnum.NorthWest => 135,
        _ => 0
    };

    public static DirectionEnum FromAngleDegrees(int angle)
    {
        // normaliza 0..360
        var a = (angle % 360 + 360) % 360;
        // cada "fatia" de 45° (centro nas direções)
        var index = (int)Math.Round(a / 45.0) % 8;
        return index switch
        {
            0 => DirectionEnum.East,
            1 => DirectionEnum.NorthEast,
            2 => DirectionEnum.North,
            3 => DirectionEnum.NorthWest,
            4 => DirectionEnum.West,
            5 => DirectionEnum.SouthWest,
            6 => DirectionEnum.South,
            7 => DirectionEnum.SouthEast,
            _ => DirectionEnum.None
        };
    }

    public static DirectionEnum ToDirectionEnum(this Coordinate directionVector)
    {
        var x = Math.Clamp(directionVector.X, -1, 1);
        var y = Math.Clamp(directionVector.Y, -1, 1);

        return (x, y) switch
        {
            (0, -1) => DirectionEnum.North,
            (1, -1) => DirectionEnum.NorthEast,
            (-1, -1) => DirectionEnum.NorthWest,
            (0, 1) => DirectionEnum.South,
            (1, 1) => DirectionEnum.SouthEast,
            (-1, 1) => DirectionEnum.SouthWest,
            (1, 0) => DirectionEnum.East,
            (-1, 0) => DirectionEnum.West,
            _ => DirectionEnum.None
        };
    }
    
    public static DirectionEnum ToDirectionEnum(this GridOffset offset)
    {
        var x = Math.Clamp((int)offset.X, -1, 1);
        var y = Math.Clamp((int)offset.Y, -1, 1);

        return (x, y) switch
        {
            (0, -1) => DirectionEnum.North,
            (1, -1) => DirectionEnum.NorthEast,
            (-1, -1) => DirectionEnum.NorthWest,
            (0, 1) => DirectionEnum.South,
            (1, 1) => DirectionEnum.SouthEast,
            (-1, 1) => DirectionEnum.SouthWest,
            (1, 0) => DirectionEnum.East,
            (-1, 0) => DirectionEnum.West,
            _ => DirectionEnum.None
        };
    }
}
