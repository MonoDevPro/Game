namespace Game.Domain.Enums;

/// <summary>
/// Direção de movimento em 8 direções.
/// Compartilhado entre cliente e servidor.
/// </summary>
public enum DirectionType : byte
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