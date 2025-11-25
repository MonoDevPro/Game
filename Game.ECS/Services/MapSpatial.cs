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
    private readonly Dictionary<SpatialPosition, List<Entity>> _grid = new();

    public void Insert(SpatialPosition position, in Entity entity)
    {
        if (!_grid.TryGetValue(position, out var list))
        {
            list = [];
            _grid[position] = list;
        }

        list.Add(entity);
    }

    public bool Remove(SpatialPosition position, in Entity entity)
    {
        if (!_grid.TryGetValue(position, out var list))
            return false;

        if (!list.Remove(entity))
            return false;

        if (list.Count == 0)
            _grid.Remove(position);

        return true;
    }

    public bool Update(SpatialPosition oldPosition, SpatialPosition newPosition, in Entity entity)
    {
        if (!Remove(oldPosition, entity))
            return false;

        Insert(newPosition, entity);
        return true;
    }

    public bool TryMove(SpatialPosition from, SpatialPosition to, in Entity entity)
    {
        // Verifica se a célula destino está vazia ou reservável
        if (HasOccupant(to))
            return false;

        return Update(from, to, entity);
    }

    public int QueryAt(SpatialPosition position, Span<Entity> results)
    {
        if (!_grid.TryGetValue(position, out var list))
            return 0;

        int count = 0;
        
        foreach (var entity in list)
        {
            if (count >= results.Length)
                return count;

            results[count++] = entity;
        }

        return count;
    }

    public int QueryArea(SpatialPosition min, SpatialPosition max, Span<Entity> results)
    {
        int count = 0;

        for (sbyte z = min.Floor; z <= max.Floor; z++)
        {
            for (int x = min.X; x <= max.X; x++)
            {
                for (int y = min.Y; y <= max.Y; y++)
                {
                    var key = new SpatialPosition(x, y, z);
                    if (!_grid.TryGetValue(key, out var list))
                        continue;

                    foreach (var entity in list)
                    {
                        if (count >= results.Length)
                            return count;
                        results[count++] = entity;
                    }
                }
            }
        }

        return count;
    }

    public void ForEachAt(SpatialPosition position, Func<Entity, bool> visitor)
    {
        if (!_grid.TryGetValue(position, out var list))
            return;

        foreach (var entity in list)
        {
            if (!visitor(entity))
                break;
        }
    }

    public void ForEachArea(SpatialPosition min, SpatialPosition max, Func<Entity, bool> visitor)
    {
        for (sbyte z = min.Floor; z <= max.Floor; z++)
        {
            for (int x = min.X; x <= max.X; x++)
            {
                for (int y = min.Y; y <= max.Y; y++)
                {
                    var key = new SpatialPosition(x, y, z);
                    if (!_grid.TryGetValue(key, out var list))
                        continue;

                    foreach (var entity in list)
                    {
                        if (!visitor(entity))
                            return;
                    }
                }
            }
        }
    }

    public bool TryGetFirstAt(SpatialPosition position, out Entity entity)
    {
        if (_grid.TryGetValue(position, out var list) && list.Count > 0)
        {
            entity = list[0];
            return true;
        }

        entity = Entity.Null;
        return false;
    }

    public void Clear()
    {
        _grid.Clear();
    }

    private bool HasOccupant(SpatialPosition position)
    {
        return _grid.ContainsKey(position) && _grid[position].Count > 0;
    }
}