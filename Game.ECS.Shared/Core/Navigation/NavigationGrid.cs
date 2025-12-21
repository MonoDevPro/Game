using System.Runtime.CompilerServices;
using Game.ECS.Shared.Components.Navigation;

namespace Game.ECS.Shared.Core.Navigation;

/// <summary>
/// Grid de navegação com suporte a ocupação de células. 
/// Thread-safe para operações de ocupação.
/// </summary>
public sealed class NavigationGrid
{
    private readonly byte[] _walkability;
    private readonly byte[] _dynamicLayer;
    private readonly int[] _occupancy;  // EntityId ocupando célula (-1 = livre)

    public int Width { get; }
    public int Height { get; }
    public float CellSize { get; }
    public int TotalCells => Width * Height;

    public static readonly int[] DirX = { 0, 1, 1, 1, 0, -1, -1, -1 };
    public static readonly int[] DirY = { -1, -1, 0, 1, 1, 1, 0, -1 };
    public static readonly float[] DirCost = { 1f, 1.414f, 1f, 1.414f, 1f, 1.414f, 1f, 1.414f };
    public static readonly bool[] IsDiagonal = { false, true, false, true, false, true, false, true };

    public NavigationGrid(int width, int height, float cellSize = 1f)
    {
        Width = width;
        Height = height;
        CellSize = cellSize;
        _walkability = new byte[width * height];
        _dynamicLayer = new byte[width * height];
        _occupancy = new int[width * height];

        Array.Fill(_walkability, (byte)1);
        Array.Fill(_occupancy, -1);
    }

    #region Coordinate Conversion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CoordToIndex(int x, int y) => y * Width + x;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CoordToIndex(GridPosition pos) => CoordToIndex(pos.X, pos.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int X, int Y) IndexToCoord(int index) => (index % Width, index / Width);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValidCoord(int x, int y)
        => (uint)x < (uint)Width && (uint)y < (uint)Height;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid(GridPosition pos)
        => IsValidCoord(pos.X, pos.Y);

    #endregion

    #region Walkability

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkable(int x, int y)
    {
        if (!IsValidCoord(x, y)) return false;
        int index = CoordToIndex(x, y);
        return _walkability[index] > 0 && _dynamicLayer[index] == 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkable(GridPosition pos)
    {
        return IsWalkable(pos.X, pos.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkableAndFree(int x, int y)
    {
        if (!IsValidCoord(x, y)) return false;
        int index = CoordToIndex(x, y);
        return _walkability[index] > 0 && 
               _dynamicLayer[index] == 0 && 
               _occupancy[index] < 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetMovementCost(int x, int y)
    {
        if (!IsValidCoord(x, y)) return float.MaxValue;
        byte cost = _walkability[CoordToIndex(x, y)];
        return cost == 0 ? float.MaxValue : cost / 255f + 1f;
    }

    public void SetWalkable(int x, int y, bool walkable)
    {
        if (IsValidCoord(x, y))
            _walkability[CoordToIndex(x, y)] = walkable ? (byte)1 : (byte)0;
    }

    public void SetCost(int x, int y, byte cost)
    {
        if (IsValidCoord(x, y))
            _walkability[CoordToIndex(x, y)] = cost;
    }

    #endregion

    #region Occupancy (Thread-Safe)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOccupied(int x, int y)
    {
        if (!IsValidCoord(x, y)) return true;
        return Volatile.Read(ref _occupancy[CoordToIndex(x, y)]) >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOccupant(int x, int y)
    {
        if (!IsValidCoord(x, y)) return -1;
        return Volatile.Read(ref _occupancy[CoordToIndex(x, y)]);
    }

    /// <summary>
    /// Tenta ocupar uma célula atomicamente.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryOccupy(GridPosition pos, int entityId)
    {
        if (!IsValid(pos)) return false;
        if (!IsWalkable(pos)) return false;

        int index = CoordToIndex(pos);
        return Interlocked.CompareExchange(ref _occupancy[index], entityId, -1) == -1;
    }

    /// <summary>
    /// Libera uma célula. 
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Release(int x, int y, int entityId)
    {
        if (!IsValidCoord(x, y)) return false;

        int index = CoordToIndex(x, y);
        return Interlocked.CompareExchange(ref _occupancy[index], -1, entityId) == entityId;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Release(GridPosition pos, int entityId)
    {
        if (!IsValid(pos)) return false;

        int index = CoordToIndex(pos);
        return Interlocked.CompareExchange(ref _occupancy[index], -1, entityId) == entityId;
    }
    

    /// <summary>
    /// Move ocupação atomicamente de uma célula para outra.
    /// </summary>
    public bool TryMoveOccupancy(int fromX, int fromY, int toX, int toY, int entityId)
    {
        if (!IsValidCoord(toX, toY)) return false;
        if (!IsWalkable(toX, toY)) return false;

        int toIndex = CoordToIndex(toX, toY);

        // Tenta ocupar destino
        if (Interlocked.CompareExchange(ref _occupancy[toIndex], entityId, -1) != -1)
            return false;

        // Libera origem
        if (IsValidCoord(fromX, fromY))
        {
            int fromIndex = CoordToIndex(fromX, fromY);
            Interlocked.CompareExchange(ref _occupancy[fromIndex], -1, entityId);
        }

        return true;
    }
    
    public bool TryMoveOccupancy(GridPosition from, GridPosition to, int entityId)
    {
        if (!IsValid(to)) return false;
        if (!IsWalkable(to)) return false;

        int toIndex = CoordToIndex(to);

        // Tenta ocupar destino
        if (Interlocked.CompareExchange(ref _occupancy[toIndex], entityId, -1) != -1)
            return false;

        // Libera origem
        if (IsValid(from))
        {
            int fromIndex = CoordToIndex(from);
            Interlocked.CompareExchange(ref _occupancy[fromIndex], -1, entityId);
        }

        return true;
    }

    /// <summary>
    /// Força liberação (para cleanup).
    /// </summary>
    public void ForceRelease(int x, int y)
    {
        if (IsValidCoord(x, y))
            Volatile.Write(ref _occupancy[CoordToIndex(x, y)], -1);
    }

    #endregion

    #region Dynamic Obstacles

    public void AddDynamicObstacle(int x, int y, int radius = 0)
    {
        ApplyToArea(x, y, radius, (idx) => _dynamicLayer[idx] = 1);
    }

    public void RemoveDynamicObstacle(int x, int y, int radius = 0)
    {
        ApplyToArea(x, y, radius, (idx) => _dynamicLayer[idx] = 0);
    }

    public void ClearDynamicLayer()
    {
        Array.Clear(_dynamicLayer);
    }

    #endregion

    #region Bulk Operations

    public void SetRectangle(int x, int y, int width, int height, bool walkable)
    {
        byte value = walkable ? (byte)1 : (byte)0;
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (IsValidCoord(nx, ny))
                    _walkability[CoordToIndex(nx, ny)] = value;
            }
        }
    }

    public void SetCircle(int centerX, int centerY, int radius, bool walkable)
    {
        byte value = walkable ? (byte)1 : (byte)0;
        ApplyToArea(centerX, centerY, radius, (idx) => _walkability[idx] = value);
    }

    private void ApplyToArea(int centerX, int centerY, int radius, Action<int> action)
    {
        if (radius == 0)
        {
            if (IsValidCoord(centerX, centerY))
                action(CoordToIndex(centerX, centerY));
            return;
        }

        int radiusSq = radius * radius;
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy <= radiusSq)
                {
                    int nx = centerX + dx, ny = centerY + dy;
                    if (IsValidCoord(nx, ny))
                        action(CoordToIndex(nx, ny));
                }
            }
        }
    }

    public void LoadFromBytes(byte[] data)
    {
        if (data.Length != _walkability.Length)
            throw new ArgumentException("Data size mismatch");
        Buffer.BlockCopy(data, 0, _walkability, 0, data.Length);
    }

    public byte[] ToBytes()
    {
        var result = new byte[_walkability.Length];
        Buffer.BlockCopy(_walkability, 0, result, 0, _walkability.Length);
        return result;
    }

    #endregion
}