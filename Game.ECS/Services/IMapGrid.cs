using Game.ECS.Components;

namespace Game.ECS.Services;

public interface IMapGrid
{
    bool InBounds(SpatialPosition spatialPosition);
    SpatialPosition ClampToBounds(SpatialPosition spatialPosition);
    bool IsBlocked(SpatialPosition spatialPosition);
    bool AnyBlockedInArea(SpatialPosition min, SpatialPosition max);
    int CountBlockedInArea(SpatialPosition min, SpatialPosition max);
    
    /// <summary>
    /// Obtém as posições vizinhas válidas (walkable) de uma posição central.
    /// </summary>
    /// <param name="center">Posição central</param>
    /// <param name="neighbors">Buffer para armazenar os vizinhos walkable</param>
    /// <param name="allowDiagonal">Se permite vizinhos diagonais</param>
    /// <returns>Quantidade de vizinhos walkable encontrados</returns>
    int GetWalkableNeighbors(SpatialPosition center, Span<SpatialPosition> neighbors, bool allowDiagonal = false);
}