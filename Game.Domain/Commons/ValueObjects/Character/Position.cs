using System.Runtime.InteropServices;
using Game.Domain.Commons.Enums;

namespace Game.Domain.Commons.ValueObjects.Character;

/// <summary>
/// Posição e direção do personagem no mundo.
/// Component ECS para representar localização espacial.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Position(int X, int Y, int Z = 0, DirectionType Direction = DirectionType.South)
{
    /// <summary>
    /// Cria uma posição com uma nova coordenada X.
    /// </summary>
    public Position WithX(int newX) => new(newX, Y, Z, Direction);
    
    /// <summary>
    /// Cria uma posição com uma nova coordenada Y.
    /// </summary>
    public Position WithY(int newY) => new(X, newY, Z, Direction);
    
    /// <summary>
    /// Cria uma posição com uma nova coordenada Z.
    /// </summary>
    public Position WithZ(int newZ) => new(X, Y, newZ, Direction);
    
    /// <summary>
    /// Cria uma posição com uma nova direção.
    /// </summary>
    public Position WithDirection(DirectionType newDirection) => new(X, Y, Z, newDirection);
    
    public static Position Zero => default;
}
