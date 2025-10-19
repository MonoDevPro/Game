using System.Drawing;
using Arch.Core;
using Game.ECS.Components;
using QuadTrees;
using QuadTrees.QTreeRect;

namespace Game.Core.MapGame.Services;

/// <summary>
/// Adapter que implementa o ISpatialIndex usando a biblioteca Splitice/QuadTrees.
/// Object pooling para reduzir alocações em queries de alta frequência.
/// **Não thread-safe** — assuma acesso de um único thread (ex.: thread principal).
/// </summary>
public class QuadTreeService(int minX, int minY, int width, int height)
{
    private class QuadTreeItem(Entity entity, Rectangle bounds) : IRectQuadStorable
    {
        public Entity Entity { get; } = entity;
        public Rectangle Rect { get; set; } = bounds;
    }

    private readonly QuadTreeRect<QuadTreeItem> _qtree = new(new Rectangle(minX, minY, width, height));
    private readonly Dictionary<Entity, QuadTreeItem> _items = new();

    // Object pool (não concorrente) para reduzir alocações em queries
    private static readonly Stack<List<Entity>> EntityListPool = new();
    private static readonly Stack<List<QuadTreeItem>> ItemListPool = new();
    private const int PoolLimit = 20;

    public void Add(Entity entity, Rectangle bounds)
    {
        if (_items.ContainsKey(entity)) return;
        var item = new QuadTreeItem(entity, bounds);
        _items[entity] = item;
        _qtree.Add(item);
    }

    public void Remove(Entity entity)
    {
        if (_items.Remove(entity, out var item))
            _qtree.Remove(item);
    }

    public bool Move(Entity entity, Position position)
    {
        if (!_items.TryGetValue(entity, out var item))
            return false;
        
        if (item.Rect.X == position.X && item.Rect.Y == position.Y)
            return true; // sem movimento
        
        item.Rect = item.Rect with { X = position.X, Y = position.Y };
        return _qtree.Move(item);
    }
    
    public void Update(Entity entity, Rectangle bounds)
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

        // Usa object pooling para lista intermediária
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

        // Usa object pooling para ambas as listas
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

    private static List<Entity> GetPooledEntityList()
    {
        if (EntityListPool.Count > 0)
        {
            var list = EntityListPool.Pop();
            list.Clear();
            return list;
        }
        return [];
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