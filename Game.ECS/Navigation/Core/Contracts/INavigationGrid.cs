using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Navigation.Core.Contracts;

/// <summary>
/// Interface para grids de navegação (permite múltiplas implementações).
/// </summary>
public interface INavigationGrid
{
    int Width { get; }
    int Height { get; }
    int Layers { get; }
    int TotalCells { get; }
    
    int CoordToIndex(int x, int y, int z = 0);
    (int X, int Y, int Z) IndexToCoord(int index);
    bool IsValidCoord(int x, int y, int z = 0);
    bool IsWalkable(int x, int y, int z = 0);
    bool IsWalkableAndFree(int x, int y, int z = 0);
    float GetMovementCost(int x, int y, int z = 0);
    bool IsOccupied(int x, int y, int z = 0);
    int GetOccupant(int x, int y, int z = 0);
    bool TryOccupy(int x, int y, int z, Entity entity);
    bool Release(int x, int y, int z, Entity entity);
    bool TryMoveOccupancy(Position from, Position to, Entity entity);
}
