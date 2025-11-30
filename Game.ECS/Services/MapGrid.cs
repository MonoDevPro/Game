using Game.ECS.Components;

namespace Game.ECS.Services;

public interface IMapGrid
{
    bool InBounds(Position position, int floor);
    (Position Position, int Floor) ClampToBounds(Position position, int floor);
    bool IsBlocked(Position position, int floor);
    bool AnyBlockedInArea(Position min, Position max, int minFloor, int maxFloor);
    int CountBlockedInArea(Position min, Position max, int minFloor, int maxFloor);
    
    /// <summary>
    /// Obtém as posições vizinhas válidas (walkable) de uma posição central.
    /// Neighbors contém apenas X/Y; o nível de piso retornado é o mesmo passado no parâmetro floor.
    /// </summary>
    /// <param name="center">Posição central</param>
    /// <param name="floor">Nível de piso (camada Z)</param>
    /// <param name="neighbors">Buffer para armazenar os vizinhos walkable (somente X/Y)</param>
    /// <param name="allowDiagonal">Se permite vizinhos diagonais</param>
    /// <returns>Quantidade de vizinhos walkable encontrados</returns>
    int GetWalkableNeighbors(Position center, int floor, Span<Position> neighbors, bool allowDiagonal = false);
}

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

    public bool InBounds(Position position, int floor)
    {
        return position.X >= 0 && position.X < _width &&
               position.Y >= 0 && position.Y < _height &&
               floor >= 0 && floor < _layers;
    }

    public (Position Position, int Floor) ClampToBounds(Position position, int floor)
    {
        var clampedX = Math.Max(0, Math.Min(position.X, _width - 1));
        var clampedY = Math.Max(0, Math.Min(position.Y, _height - 1));
        var clampedFloor = Math.Max(0, Math.Min(floor, _layers - 1));
        return (new Position { X = clampedX, Y = clampedY }, clampedFloor);
    }

    public bool IsBlocked(Position position, int floor)
    {
        if (!InBounds(position, floor))
            return true; // Fora do mapa é considerado bloqueado

        return _blocked[position.X, position.Y, floor];
    }

    public bool AnyBlockedInArea(Position min, Position max, int minFloor, int maxFloor)
    {
        int minX = Math.Max(0, Math.Min(min.X, max.X));
        int maxX = Math.Min(_width - 1, Math.Max(min.X, max.X));
        int minY = Math.Max(0, Math.Min(min.Y, max.Y));
        int maxY = Math.Min(_height - 1, Math.Max(min.Y, max.Y));
        int minLayer = Math.Max(0, Math.Min(minFloor, maxFloor));
        int maxLayer = Math.Min(_layers - 1, Math.Max(minFloor, maxFloor));

        for (int z = minLayer; z <= maxLayer; z++)
            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                    if (_blocked[x, y, z])
                        return true;
        return false;
    }

    public int CountBlockedInArea(Position min, Position max, int minFloor, int maxFloor)
    {
        int minX = Math.Max(0, Math.Min(min.X, max.X));
        int maxX = Math.Min(_width - 1, Math.Max(min.X, max.X));
        int minY = Math.Max(0, Math.Min(min.Y, max.Y));
        int maxY = Math.Min(_height - 1, Math.Max(min.Y, max.Y));
        int minLayer = Math.Max(0, Math.Min(minFloor, maxFloor));
        int maxLayer = Math.Min(_layers - 1, Math.Max(minFloor, maxFloor));

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
    public void SetBlocked(Position position, int floor, bool blocked)
    {
        if (InBounds(position, floor))
            _blocked[position.X, position.Y, floor] = blocked;
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
    /// Retorna apenas Position (X/Y); o floor é o mesmo do parâmetro.
    /// </summary>
    public int GetWalkableNeighbors(Position center, int floor, Span<Position> neighbors, bool allowDiagonal = false)
    {
        var directions = allowDiagonal ? AllDirections : CardinalDirections;
        int count = 0;
        
        for (int i = 0; i < directions.Length && count < neighbors.Length; i++)
        {
            var (dx, dy) = directions[i];
            var neighborPos = new Position { X = center.X + dx, Y = center.Y + dy };
            
            if (InBounds(neighborPos, floor) && !IsBlocked(neighborPos, floor))
            {
                // Se diagonal, verifica se os tiles adjacentes estão livres (evita cortar cantos)
                if (allowDiagonal && dx != 0 && dy != 0)
                {
                    if (IsBlocked(new Position { X = center.X + dx, Y = center.Y }, floor) ||
                        IsBlocked(new Position { X = center.X, Y = center.Y + dy }, floor))
                        continue;
                }
                
                neighbors[count++] = neighborPos;
            }
        }
        
        return count;
    }
}