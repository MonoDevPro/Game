using Game.ECS.Components;

namespace Game.ECS.Services;

/// <summary>
/// Implementação padrão de IMapGrid para gerenciar limites e bloqueios de mapa.
/// </summary>
public class MapGrid(int width, int height, bool[,]? blockedCells = null) : IMapGrid
{
    private readonly bool[,] _blocked = blockedCells ?? new bool[width, height];

    public bool InBounds(Position p)
    {
        return p.X >= 0 && p.X < width && p.Y >= 0 && p.Y < height;
    }

    public Position ClampToBounds(Position p)
    {
        return new Position
        {
            X = Math.Max(0, Math.Min(p.X, width - 1)),
            Y = Math.Max(0, Math.Min(p.Y, height - 1)),
            Z = p.Z
        };
    }

    public bool IsBlocked(Position p)
    {
        if (!InBounds(p))
            return true; // Fora do mapa é considerado bloqueado

        return _blocked[p.X, p.Y];
    }

    public bool AnyBlockedInArea(Position min, Position max)
    {
        int minX = Math.Max(0, Math.Min(min.X, max.X));
        int maxX = Math.Min(width - 1, Math.Max(min.X, max.X));
        int minY = Math.Max(0, Math.Min(min.Y, max.Y));
        int maxY = Math.Min(height - 1, Math.Max(min.Y, max.Y));

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (_blocked[x, y])
                    return true;
            }
        }

        return false;
    }

    public int CountBlockedInArea(Position min, Position max)
    {
        int minX = Math.Max(0, Math.Min(min.X, max.X));
        int maxX = Math.Min(width - 1, Math.Max(min.X, max.X));
        int minY = Math.Max(0, Math.Min(min.Y, max.Y));
        int maxY = Math.Min(height - 1, Math.Max(min.Y, max.Y));

        int count = 0;
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (_blocked[x, y])
                    count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Define se uma célula está bloqueada.
    /// </summary>
    public void SetBlocked(Position p, bool blocked)
    {
        if (InBounds(p))
        {
            _blocked[p.X, p.Y] = blocked;
        }
    }

    /// <summary>
    /// Obtém as dimensões do mapa.
    /// </summary>
    public (int Width, int Height) GetDimensions() => (_width: width, _height: height);
}
