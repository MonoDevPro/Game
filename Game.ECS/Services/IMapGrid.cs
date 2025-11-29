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