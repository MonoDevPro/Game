using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Services;

/// <summary>
/// Implementação de spatial hashing para IMapSpatial.
/// Permite queries rápidas de entidades por posição.
/// </summary>
public class MapSpatial : IMapSpatial
{
    private readonly Dictionary<(int x, int y), List<Entity>> _grid = [];
    private readonly HashSet<Entity> _reserved = [];
    private readonly Dictionary<Entity, uint> _reserveVersions = [];
    private uint _globalVersion;

    public void Insert(Position position, in Entity entity)
    {
        var key = (position.X, position.Y);
        if (!_grid.ContainsKey(key))
        {
            _grid[key] = [];
        }

        _grid[key].Add(entity);
    }

    public bool Remove(Position position, in Entity entity)
    {
        var key = (position.X, position.Y);
        if (!_grid.TryGetValue(key, out var list))
            return false;

        if (!list.Remove(entity))
            return false;

        if (list.Count == 0)
            _grid.Remove(key);

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

    public int QueryAt(Position position, Span<Entity> results)
    {
        var key = (position.X, position.Y);
        if (!_grid.TryGetValue(key, out var list))
            return 0;

        int count = 0;
        foreach (var entity in list)
        {
            if (count >= results.Length)
                break;

            results[count++] = entity;
        }

        return count;
    }

    public int QueryArea(Position minInclusive, Position maxInclusive, Span<Entity> results)
    {
        int count = 0;
        int minX = Math.Min(minInclusive.X, maxInclusive.X);
        int maxX = Math.Max(minInclusive.X, maxInclusive.X);
        int minY = Math.Min(minInclusive.Y, maxInclusive.Y);
        int maxY = Math.Max(minInclusive.Y, maxInclusive.Y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var key = (x, y);
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

        return count;
    }

    public void ForEachAt(Position position, Func<Entity, bool> visitor)
    {
        var key = (position.X, position.Y);
        if (!_grid.TryGetValue(key, out var list))
            return;

        foreach (var entity in list)
        {
            if (!visitor(entity))
                break;
        }
    }

    public void ForEachArea(Position minInclusive, Position maxInclusive, Func<Entity, bool> visitor)
    {
        int minX = Math.Min(minInclusive.X, maxInclusive.X);
        int maxX = Math.Max(minInclusive.X, maxInclusive.X);
        int minY = Math.Min(minInclusive.Y, maxInclusive.Y);
        int maxY = Math.Max(minInclusive.Y, maxInclusive.Y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var key = (x, y);
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

    public bool TryGetFirstAt(Position position, out Entity entity)
    {
        var key = (position.X, position.Y);
        if (_grid.TryGetValue(key, out var list) && list.Count > 0)
        {
            entity = list[0];
            return true;
        }

        entity = Entity.Null;
        return false;
    }

    public bool TryReserve(Position position, in Entity reserver, out ReservationToken token)
    {
        token = default;

        // Se há ocupante, não pode reservar
        if (HasOccupant(position))
            return false;

        _reserved.Add(reserver);
        _globalVersion++;
        _reserveVersions[reserver] = _globalVersion;

        token = new ReservationToken(position, reserver, _globalVersion);
        return true;
    }

    public bool ReleaseReservation(ReservationToken token)
    {
        // Verifica se o token ainda é válido (não foi double-freed)
        if (!_reserveVersions.TryGetValue(token.Reserver, out var version))
            return false;

        if (version != token.Version)
            return false; // Token expirou (versão diferente)

        _reserved.Remove(token.Reserver);
        _reserveVersions.Remove(token.Reserver);

        return true;
    }

    public void Clear()
    {
        _grid.Clear();
        _reserved.Clear();
        _reserveVersions.Clear();
        _globalVersion = 0;
    }

    private bool HasOccupant(Position position)
    {
        var key = (position.X, position.Y);
        return _grid.ContainsKey(key) && _grid[key].Count > 0;
    }
}
