using Game.ECS.Components;

namespace Game.ECS.Services;

/// <summary>
/// Implementação padrão de IMapGrid para gerenciar limites e bloqueios de mapa.
/// </summary>
/// <summary>
/// Implementação padrão de IMapGrid para gerenciar limites e bloqueios de mapa (agora com suporte a camadas Z).
/// </summary>
public class MapGrid : IMapGrid
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _layers;
    // blocked[x,y,z]
    private readonly bool[,,] _blocked;

    public MapGrid(int width, int height, int layers = 1, bool[,,]? blockedCells = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(layers);
        _width = width;
        _height = height;
        _layers = layers;
        _blocked = blockedCells ?? new bool[width, height, layers];
    }

    public bool InBounds(SpatialPosition position)
    {
        return position.X >= 0 && position.X < _width &&
               position.Y >= 0 && position.Y < _height &&
               position.Floor >= 0 && position.Floor < _layers;
    }

    public SpatialPosition ClampToBounds(SpatialPosition position)
    {
        return new SpatialPosition(
            Math.Max(0, Math.Min(position.X, _width - 1)), 
            Math.Max(0, Math.Min(position.Y, _height - 1)), 
            (sbyte)Math.Max(0, Math.Min(position.Floor, _layers - 1)));
    }

    public bool IsBlocked(SpatialPosition position)
    {
        if (!InBounds(position))
            return true; // Fora do mapa é considerado bloqueado

        return _blocked[position.X, position.Y, position.Floor];
    }

    public bool AnyBlockedInArea(SpatialPosition min, SpatialPosition max)
    {
        int minX = Math.Max(0, Math.Min(min.X, max.X));
        int maxX = Math.Min(_width - 1, Math.Max(min.X, max.X));
        int minY = Math.Max(0, Math.Min(min.Y, max.Y));
        int maxY = Math.Min(_height - 1, Math.Max(min.Y, max.Y));
        int minLayer = Math.Max((sbyte)0, Math.Min(min.Floor, max.Floor));
        int maxLayer = Math.Min(_layers - 1, Math.Max(min.Floor, max.Floor));

        for (int z = minLayer; z <= maxLayer; z++)
            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                    if (_blocked[x, y, z])
                        return true;
        return false;
    }

    public int CountBlockedInArea(SpatialPosition min, SpatialPosition max)
    {
        int minX = Math.Max(0, Math.Min(min.X, max.X));
        int maxX = Math.Min(_width - 1, Math.Max(min.X, max.X));
        int minY = Math.Max(0, Math.Min(min.Y, max.Y));
        int maxY = Math.Min(_height - 1, Math.Max(min.Y, max.Y));
        int minLayer = Math.Max((sbyte)0, Math.Min(min.Floor, max.Floor));
        int maxLayer = Math.Min(_layers - 1, Math.Max(min.Floor, max.Floor));

        int count = 0;
        for (int z = minLayer; z <= maxLayer; z++)
            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                    if (_blocked[x, y, z])
                        count++;
        return count;
    }

    /// <summary>
    /// Define se uma célula está bloqueada.
    /// </summary>
    public void SetBlocked(SpatialPosition position, bool blocked)
    {
        if (InBounds(position))
            _blocked[position.X, position.Y, position.Floor] = blocked;
    }

    /// <summary>
    /// Obtém as dimensões do mapa.
    /// </summary>
    public (int Width, int Height, int Layers) GetDimensions() => (_width, _height, _layers);
    
    // Direções cardinais (4 direções)
    private static readonly (int dx, int dy)[] CardinalDirections =
    [
        (0, -1),  // Norte
        (1, 0),   // Leste
        (0, 1),   // Sul
        (-1, 0)   // Oeste
    ];
    
    // Direções 8-way (inclui diagonais)
    private static readonly (int dx, int dy)[] AllDirections =
    [
        (0, -1),   // Norte
        (1, -1),   // Nordeste
        (1, 0),    // Leste
        (1, 1),    // Sudeste
        (0, 1),    // Sul
        (-1, 1),   // Sudoeste
        (-1, 0),   // Oeste
        (-1, -1)   // Noroeste
    ];
    
    /// <summary>
    /// Obtém as posições vizinhas válidas (walkable) de uma posição central.
    /// </summary>
    public int GetWalkableNeighbors(SpatialPosition center, Span<SpatialPosition> neighbors, bool allowDiagonal = false)
    {
        var directions = allowDiagonal ? AllDirections : CardinalDirections;
        int count = 0;
        
        for (int i = 0; i < directions.Length && count < neighbors.Length; i++)
        {
            var (dx, dy) = directions[i];
            var neighbor = new SpatialPosition(center.X + dx, center.Y + dy, center.Floor);
            
            if (InBounds(neighbor) && !IsBlocked(neighbor))
            {
                // Se diagonal, verifica se os tiles adjacentes estão livres (evita cortar cantos)
                if (allowDiagonal && dx != 0 && dy != 0)
                {
                    if (IsBlocked(new SpatialPosition(center.X + dx, center.Y, center.Floor)) ||
                        IsBlocked(new SpatialPosition(center.X, center.Y + dy, center.Floor)))
                        continue;
                }
                
                neighbors[count++] = neighbor;
            }
        }
        
        return count;
    }
}