using System.Drawing;
using Arch.Core;
using QuadTrees;
using QuadTrees.QTreeRect;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Services;
using Simulation.Core.Ports.ECS;

namespace Simulation.Core.ECS.Resource;

/// <summary>
/// Resource que encapsula o índice espacial (QuadTree) e regras de mapa (colisão/bordas).
/// Único ponto de escrita deve ser o SpatialIndexSystem.
/// Não thread-safe — assume acesso no thread principal do mundo.
/// </summary>
public sealed class SpatialIndexResource(MapService map) : ISpatialIndex
{
    private sealed class QuadTreeItem(Entity entity, Rectangle bounds) : IRectQuadStorable
    {
        public Entity Entity { get; } = entity;
        public Rectangle Rect { get; set; } = bounds;
    }

    private readonly QuadTreeRect<QuadTreeItem> _qtree = new(new Rectangle(0, 0, map.Width, map.Height));
    private readonly Dictionary<Entity, QuadTreeItem> _items = new();

    // Object pool (não concorrente) para reduzir alocações em queries frequentes
    private static readonly Stack<List<Entity>> EntityListPool = new();
    private static readonly Stack<List<QuadTreeItem>> ItemListPool = new();
    private const int PoolLimit = 20;

    // Bounds do mundo de acordo com o mapa (0..Width, 0..Height)

    public void Add(in Entity entity, in Position pos)
    {
        var rect = new Rectangle(pos.X, pos.Y, 0, 0);
        
        if (_items.TryGetValue(entity, out var value))
        {
            if (value.Rect == rect) return;
            value.Rect = rect;
            _qtree.Move(value);
            return;
        }
        
        var item = new QuadTreeItem(entity, rect);
        _items[entity] = item;
        _qtree.Add(item);
    }

    public void Remove(in Entity entity)
    {
        if (_items.Remove(entity, out var item))
            _qtree.Remove(item);
    }

    public bool Move(in Entity entity, in Position newPos)
    {
        // Validação centralizada com o mapa
        if (map.IsBlocked(newPos)) return false;

        if (!_items.TryGetValue(entity, out var item))
            return false;

        if (item.Rect.X == newPos.X && item.Rect.Y == newPos.Y)
            return true; // sem movimento

        item.Rect = item.Rect with { X = newPos.X, Y = newPos.Y };
        return _qtree.Move(item);
    }

    /// <summary>
    /// Atualiza bounds; útil se o tamanho do "occupant" mudar. Opcional.
    /// </summary>
    public void UpdateBounds(in Entity entity, Rectangle bounds)
    {
        if (!_items.TryGetValue(entity, out var item))
            return;

        if (_qtree.Remove(item))
        {
            item.Rect = bounds;
            _qtree.Add(item);
        }
    }

    public void Query(Position center, int radius, List<Entity> results)
    {
        var searchRect = new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2);

        var itemResults = GetPooledItemList();
        try
        {
            _qtree.GetObjects(searchRect, itemResults);

            results.Clear();
            foreach (var item in itemResults)
                results.Add(item.Entity);
        }
        finally
        {
            ReturnPooledItemList(itemResults);
        }
    }

    public List<Entity> Query(Position center, int radius)
    {
        var searchRect = new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2);

        var itemResults = GetPooledItemList();
        var entityResults = GetPooledEntityList();

        try
        {
            _qtree.GetObjects(searchRect, itemResults);

            foreach (var item in itemResults)
                entityResults.Add(item.Entity);

            // Retorna uma nova lista para evitar problemas de ownership
            return new List<Entity>(entityResults);
        }
        finally
        {
            ReturnPooledItemList(itemResults);
            ReturnPooledEntityList(entityResults);
        }
    }

    // --- pooling helpers ---

    private static List<Entity> GetPooledEntityList()
    {
        if (EntityListPool.Count > 0)
        {
            var list = EntityListPool.Pop();
            list.Clear();
            return list;
        }
        return new List<Entity>();
    }

    private static void ReturnPooledEntityList(List<Entity> list)
    {
        if (EntityListPool.Count < PoolLimit)
        {
            list.Clear();
            EntityListPool.Push(list);
        }
    }

    private static List<QuadTreeItem> GetPooledItemList()
    {
        if (ItemListPool.Count > 0)
        {
            var list = ItemListPool.Pop();
            list.Clear();
            return list;
        }
        return new List<QuadTreeItem>();
    }

    private static void ReturnPooledItemList(List<QuadTreeItem> list)
    {
        if (ItemListPool.Count < PoolLimit)
        {
            list.Clear();
            ItemListPool.Push(list);
        }
    }
}