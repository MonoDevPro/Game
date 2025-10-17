using Game.Domain.Enums;

namespace Game.Domain.Extensions;

public static class FacingHelpers
{
    public static FacingEnum ToFacingEnum(int directionX, int directionY)
    {
        return (directionX, directionY) switch
        {
            (0, -1) => FacingEnum.North,
            (1, -1) => FacingEnum.NorthEast,
            (-1, -1) => FacingEnum.NorthWest,
            (0, 1) => FacingEnum.South,
            (1, 1) => FacingEnum.SouthEast,
            (-1, 1) => FacingEnum.SouthWest,
            (1, 0) => FacingEnum.East,
            (-1, 0) => FacingEnum.West,
            _ => FacingEnum.None
        };
    }
    
    public static (int directionX, int directionY) FacingToDirections(this FacingEnum d)
    {
        return d switch
        {
            FacingEnum.North => (0, -1),
            FacingEnum.NorthEast => (1, -1),
            FacingEnum.East => (1, 0),
            FacingEnum.SouthEast => (1, 1),
            FacingEnum.South => (0, 1),
            FacingEnum.SouthWest => (-1, 1),
            FacingEnum.West => (-1, 0),
            FacingEnum.NorthWest => (-1, -1),
            _ => (0, 0)
        };
    }
    
    public static int FacingToAngle(this FacingEnum d) => d switch
    {
        FacingEnum.North => 90,
        FacingEnum.NorthEast => 45,
        FacingEnum.East => 0,
        FacingEnum.SouthEast => 315,
        FacingEnum.South => 270,
        FacingEnum.SouthWest => 225,
        FacingEnum.West => 180,
        FacingEnum.NorthWest => 135,
        _ => 0
    };
    
    public static FacingEnum AngleToFacing(int angle)
    {
        // normaliza 0..360
        var a = (angle % 360 + 360) % 360;
        // cada "fatia" de 45° (centro nas direções)
        var index = (int)Math.Round(a / 45.0) % 8;
        return index switch
        {
            0 => FacingEnum.East,
            1 => FacingEnum.NorthEast,
            2 => FacingEnum.North,
            3 => FacingEnum.NorthWest,
            4 => FacingEnum.West,
            5 => FacingEnum.SouthWest,
            6 => FacingEnum.South,
            7 => FacingEnum.SouthEast,
            _ => FacingEnum.None
        };
    }
}