using System.Runtime.InteropServices;
using Game.Domain.Enums;

namespace Game.Domain.ValueObjects.Character;

/// <summary>
/// Posição e direção do personagem no mundo.
/// Component ECS para representar localização espacial.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Position
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Z { get; init; }
    public DirectionType Direction { get; init; }

    public Position(int x, int y, int z = 0, DirectionType direction = DirectionType.South)
    {
        X = x;
        Y = y;
        Z = z;
        Direction = direction;
    }

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
    
    /// <summary>
    /// Calcula a distância Manhattan até outra posição.
    /// </summary>
    public int ManhattanDistance(Position other) => 
        Math.Abs(X - other.X) + Math.Abs(Y - other.Y) + Math.Abs(Z - other.Z);
    
    /// <summary>
    /// Calcula a distância Euclidiana até outra posição.
    /// </summary>
    public double EuclideanDistance(Position other)
    {
        int dx = X - other.X;
        int dy = Y - other.Y;
        int dz = Z - other.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
    
    public static Position Zero => default;
}
