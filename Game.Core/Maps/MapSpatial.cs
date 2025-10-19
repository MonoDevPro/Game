using System.Drawing;
using System.Runtime.CompilerServices;
using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Services;
using QuadTrees;
using QuadTrees.QTreeRect;

namespace Game.Core.Maps;

/// <summary>
/// Implementação de ISpatialService usando Splitice/QuadTrees (QuadTrees.QTreeRect).
/// - Sem alocações em hot path: consultas preenchem Span<Entity> ou usam visitor
/// - Move/Update atômicos usam suporte do QuadTreeRect.Move
/// - Reservas com token versionado para segurança
/// - Não thread-safe (use em um único thread, ex.: thread principal da simulação)
/// </summary>
public sealed class MapSpatial(int minX, int minY, int width, int height)
    : IMapSpatial
{
    private sealed class QuadItem(in Entity entity, in Rectangle rect) : IRectQuadStorable
    {
        public Entity Entity { get; } = entity;
        public Rectangle Rect { get; set; } = rect;
    }

    private readonly QuadTreeRect<QuadItem> _tree = new(new Rectangle(minX, minY, width, height));
    private readonly Dictionary<Entity, QuadItem> _index = new();              // Entity -> Item
    private readonly Dictionary<long, Reservation> _reservations = new();      // packed(x,y) -> reservation/version

    private struct Reservation
    {
        public Entity Reserver;
        public uint Version;
    }

    // Pool não concorrente para listas temporárias (evita GC em consultas)
    private readonly Stack<List<QuadItem>> _itemListPool = new();
    private const int PoolLimit = 32;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Rectangle CellRect(in Position p) => new(p.X, p.Y, 1, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Rectangle AreaRect(in Position minInclusive, in Position maxInclusive)
        => new(minInclusive.X, minInclusive.Y, (maxInclusive.X - minInclusive.X + 1), (maxInclusive.Y - minInclusive.Y + 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long PackXY(int x, int y) => ((long)(uint)y << 32) | (uint)x;

    private List<QuadItem> RentItemList()
    {
        if (_itemListPool.Count > 0)
        {
            var l = _itemListPool.Pop();
            l.Clear();
            return l;
        }
        return new List<QuadItem>(64);
    }

    private void ReturnItemList(List<QuadItem> list)
    {
        if (_itemListPool.Count < PoolLimit)
        {
            list.Clear();
            _itemListPool.Push(list);
        }
    }

    public void Insert(in Position position, in Entity entity)
    {
        if (_index.ContainsKey(entity)) return;

        var rect = CellRect(in position);
        var item = new QuadItem(entity, rect);
        _index[entity] = item;
        _tree.Add(item);
    }

    public bool Remove(in Position position, in Entity entity)
    {
        if (!_index.TryGetValue(entity, out var item))
            return false;

        // Não dependemos de "position" para remover; usamos o item indexado
        var removed = _tree.Remove(item);
        _index.Remove(entity);
        return removed;
    }

    public bool Update(in Position oldPosition, in Position newPosition, in Entity entity)
    {
        if (!_index.TryGetValue(entity, out var item))
            return false;

        var oldX = item.Rect.X; var oldY = item.Rect.Y;
        if (oldX == newPosition.X && oldY == newPosition.Y)
            return true; // nada a fazer

        item.Rect = item.Rect with { X = newPosition.X, Y = newPosition.Y, Width = 1, Height = 1 };
        // Move reposiciona o item no quadtree adequadamente
        return _tree.Move(item);
    }

    public bool TryMove(in Position from, in Position to, in Entity entity)
    {
        if (!_index.TryGetValue(entity, out var item))
            return false;

        // Checagem rápida: já está lá?
        if (item.Rect.X == to.X && item.Rect.Y == to.Y)
            return true;

        // Opcional: negar movimento se célula estiver ocupada por outra entidade
        // (fast-path usando QueryAt com buffer pequeno)
        Span<Entity> scratch = stackalloc Entity[1];
        var occupants = QueryAt(in to, scratch);
        if (occupants > 0 && scratch[0] != entity)
            return false; // célula ocupada

        // Atualiza o retângulo e move
        item.Rect = item.Rect with { X = to.X, Y = to.Y, Width = 1, Height = 1 };
        return _tree.Move(item);
    }

    public int QueryAt(in Position position, Span<Entity> results)
    {
        var rect = CellRect(in position);
        var items = RentItemList();
        try
        {
            _tree.GetObjects(rect, items);
            int count = Math.Min(results.Length, items.Count);
            for (int i = 0; i < count; i++)
                results[i] = items[i].Entity;
            return count;
        }
        finally
        {
            ReturnItemList(items);
        }
    }

    public int QueryArea(in Position minInclusive, in Position maxInclusive, Span<Entity> results)
    {
        var rect = AreaRect(in minInclusive, in maxInclusive);
        var items = RentItemList();
        try
        {
            _tree.GetObjects(rect, items);
            int count = Math.Min(results.Length, items.Count);
            for (int i = 0; i < count; i++)
                results[i] = items[i].Entity;
            return count;
        }
        finally
        {
            ReturnItemList(items);
        }
    }

    public void ForEachAt(in Position position, Func<Entity, bool> visitor)
    {
        var rect = CellRect(in position);
        var items = RentItemList();
        try
        {
            _tree.GetObjects(rect, items);
            for (int i = 0; i < items.Count; i++)
            {
                if (!visitor(items[i].Entity))
                    break;
            }
        }
        finally
        {
            ReturnItemList(items);
        }
    }

    public void ForEachArea(in Position minInclusive, in Position maxInclusive, Func<Entity, bool> visitor)
    {
        var rect = AreaRect(in minInclusive, in maxInclusive);
        var items = RentItemList();
        try
        {
            _tree.GetObjects(rect, items);
            for (int i = 0; i < items.Count; i++)
            {
                if (!visitor(items[i].Entity))
                    break;
            }
        }
        finally
        {
            ReturnItemList(items);
        }
    }

    public bool TryGetFirstAt(in Position position, out Entity entity)
    {
        var rect = CellRect(in position);
        var items = RentItemList();
        try
        {
            _tree.GetObjects(rect, items);
            if (items.Count > 0)
            {
                entity = items[0].Entity;
                return true;
            }
        }
        finally
        {
            ReturnItemList(items);
        }
        entity = default;
        return false;
    }

    public bool TryReserve(in Position position, in Entity reserver, out ReservationToken token)
    {
        var key = PackXY(position.X, position.Y);
        if (_reservations.TryGetValue(key, out var r))
        {
            // Se já reservado por outro, falha
            if (!r.Reserver.Equals(reserver))
            {
                token = default;
                return false;
            }

            // Mesmo reservador: bump de versão (novo token)
            r.Version++;
            _reservations[key] = r;
            token = new ReservationToken(position, reserver, r.Version);
            return true;
        }
        else
        {
            // Nova reserva
            var nr = new Reservation { Reserver = reserver, Version = 1 };
            _reservations[key] = nr;
            token = new ReservationToken(position, reserver, 1);
            return true;
        }
    }

    public bool ReleaseReservation(in ReservationToken token)
    {
        var key = PackXY(token.Position.X, token.Position.Y);
        if (!_reservations.TryGetValue(key, out var r))
            return false;

        // Token inválido (outdated) ou de outro reservador
        if (!r.Reserver.Equals(token.Reserver) || r.Version != token.Version)
            return false;

        _reservations.Remove(key);
        return true;
    }

    public void Clear()
    {
        _tree.Clear();
        _index.Clear();
        _reservations.Clear();
        // Pool fica para reuso; não limpamos stacks para evitar churn
    }
}