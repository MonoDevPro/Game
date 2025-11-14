using Arch.Core;
using Arch.LowLevel;
using Game.ECS.Components;

namespace Game.ECS.Services;

/// <summary>
/// Implementação de spatial hashing para IMapSpatial.
/// Permite queries rápidas de entidades por posição.
/// </summary>
/// <summary>
/// Spatial hashing / grid para entidades considerando X,Y,Z.
/// </summary>
public class MapSpatial : IMapSpatial
{
    private readonly Dictionary<Position, UnsafeStack<Entity>> _grid = new();
    
    private const int InitialCapacityPerCell = 4;

    public void Insert(Position position, in Entity entity)
    {
        if (!_grid.TryGetValue(position, out var list))
        {
            list = new UnsafeStack<Entity>(InitialCapacityPerCell);
            _grid[position] = list;
        }

        list.Push(entity);
    }

    public bool Remove(Position position, in Entity entity)
    {
        if (!_grid.TryGetValue(position, out var list))
            return false;
        
        var stackCopy = new UnsafeStack<Entity>(list.Capacity);
        bool found = false;
        while (list.Count > 0)
        {
            var current = list.Pop();
            if (current.Equals(entity) && !found)
            {
                found = true;
                continue;
            }
            stackCopy.Push(current);
        }
        while (stackCopy.Count > 0)
            list.Push(stackCopy.Pop());
        
        if (list.Count == 0)
            _grid.Remove(position);

        return true;
    }

    public bool Update(Position oldPosition, Position newPosition, in Entity entity)
    {
        if (!Remove(oldPosition, entity))
            return false;

        Insert(newPosition, entity);
        return true;
    }

    public bool TryMove(Position from, Position to, in Entity entity)
    {
        // Verifica se a célula destino está vazia ou reservável
        if (HasOccupant(to))
            return false;

        return Update(from, to, entity);
    }

    public int QueryAt(Position position, ref UnsafeStack<Entity> results)
    {
        if (!_grid.TryGetValue(position, out var list))
            return 0;

        int count = 0;
        foreach (var e in list)
        {
            results.Push(e);
            count++;
        }

        return count;
    }

    public int QueryArea(AreaPosition area, ref UnsafeStack<Entity> results)
    {
        int count = 0;

        for (int z = area.MinZ; z <= area.MaxZ; z++)
        for (int x = area.MinX; x <= area.MaxX; x++)
        for (int y = area.MinY; y <= area.MaxY; y++)
        {
            var key = new Position(x, y, z);
            if (!_grid.TryGetValue(key, out var list))
                continue;

            foreach (var entity in list)
            {
                if (count >= results.Count)
                    return count;
                results.Push(entity);
                count++;
            }
        }

        return count;
    }

    public void ForEachAt(Position position, Func<Entity, bool> visitor)
    {
        if (!_grid.TryGetValue(position, out var list))
            return;

        foreach (var entity in list)
            if (!visitor(entity))
                break;
    }

    public void ForEachArea(AreaPosition area, Func<Entity, bool> visitor)
    {
        for (int z = area.MinZ; z <= area.MaxZ; z++)
        for (int x = area.MinX; x <= area.MaxX; x++)
        for (int y = area.MinY; y <= area.MaxY; y++)
        {
            var key = new Position(x, y, z);
            if (!_grid.TryGetValue(key, out var list))
                continue;

            foreach (var entity in list)
                if (!visitor(entity))
                    return;
        }
    }

    public bool TryGetFirstAt(Position position, out Entity entity)
    {
        if (_grid.TryGetValue(position, out var list) && list.Count > 0)
        {
            entity = list.Peek();
            return true;
        }

        entity = Entity.Null;
        return false;
    }

    public void Clear()
    {
        _grid.Clear();
    }

    private bool HasOccupant(Position position)
    {
        return _grid.ContainsKey(position) && _grid[position].Count > 0;
    }
}