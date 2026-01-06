using System.Runtime.CompilerServices;
using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Navigation.Core.Contracts;
using Game.ECS.Services.Map;

namespace Game.ECS.Navigation.Core;

/// <summary>
/// Adaptador para usar MapGrid/MapSpatial existente como grid de navegação.
/// Integra com o sistema de mapas já existente (MapIndex).
/// </summary>
public sealed class NavigationGridAdapter : INavigationGrid
{
    private readonly int _mapId;
    private readonly IMapGrid _mapGrid;
    private readonly IMapSpatial _mapSpatial;
    private readonly int _width;
    private readonly int _height;
    private readonly int _layers;

    public int Width => _width;
    public int Height => _height;
    public int Layers => _layers;
    public int TotalCells => _width * _height;

    public static readonly int[] DirX = [0, 1, 1, 1, 0, -1, -1, -1];
    public static readonly int[] DirY = [-1, -1, 0, 1, 1, 1, 0, -1];
    public static readonly float[] DirCost = [1f, 1.414f, 1f, 1.414f, 1f, 1.414f, 1f, 1.414f];
    public static readonly bool[] IsDiagonal = [false, true, false, true, false, true, false, true];

    public NavigationGridAdapter(int mapId, IMapGrid mapGrid, IMapSpatial mapSpatial)
    {
        _mapId = mapId;
        _mapGrid = mapGrid;
        _mapSpatial = mapSpatial;
        (_width, _height, _layers) = ((MapGrid)mapGrid).GetDimensions();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CoordToIndex(int x, int y, int z) => y * _width + x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int X, int Y, int Z) IndexToCoord(int index) => (index % _width, index / _width, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValidCoord(int x, int y, int z = 0)
        => (uint)x < (uint)_width && (uint)y < (uint)_height && (uint)z < (uint)_layers;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkable(int x, int y, int z = 0)
    {
        var pos = new Position { X = x, Y = y, Z = z };
        return _mapGrid.InBounds(pos) && !_mapGrid.IsBlocked(pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkableAndFree(int x, int y, int z = 0)
    {
        if (!IsWalkable(x, y, z)) return false;
        var pos = new Position { X = x, Y = y, Z = z };
        return !_mapSpatial.TryGetFirstAt(pos, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetMovementCost(int x, int y, int z = 0)
    {
        // Custo fixo de 1.0 por célula (pode ser expandido para terreno variável)
        return IsWalkable(x, y, z) ? 1f : float.MaxValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOccupied(int x, int y, int z = 0)
    {
        var pos = new Position { X = x, Y = y, Z = z };
        return _mapSpatial.TryGetFirstAt(pos, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOccupant(int x, int y, int z = 0)
    {
        var pos = new Position { X = x, Y = y, Z = z };
        if (_mapSpatial.TryGetFirstAt(pos, out var entity))
            return entity.Id;
        return -1;
    }

    /// <summary>
    /// Tenta ocupar uma célula usando MapSpatial.
    /// </summary>
    public bool TryOccupy(int x, int y, int z, Entity entity)
    {
        if (!IsWalkable(x, y, z)) return false;
        var pos = new Position { X = x, Y = y, Z = z };
        if (_mapSpatial.TryGetFirstAt(pos, out _)) return false;
        _mapSpatial.Insert(pos, entity);
        return true;
    }

    /// <summary>
    /// Libera uma célula.
    /// </summary>
    public bool Release(int x, int y, int z, Entity entity)
    {
        var pos = new Position { X = x, Y = y, Z = z };
        return _mapSpatial.Remove(pos, entity);
    }

    /// <summary>
    /// Move ocupação atomicamente de uma célula para outra.
    /// </summary>
    public bool TryMoveOccupancy(Position from, Position to, Entity entity)
    {
        return _mapSpatial.TryMove(from, to, entity);
    }
}