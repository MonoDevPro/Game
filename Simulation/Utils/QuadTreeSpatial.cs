using System.Collections.Concurrent;
using System.Drawing;
using Arch.Core;
using QuadTrees;
using QuadTrees.QTreeRect;
using Simulation.Domain;

namespace Simulation.ECS.Utils;

public interface ISpatialIndex
{
    void Add(Entity entity, Position position);
    void Remove(Entity entity);
    void Update(Entity entity, Position newPosition);
    void Query(Position center, int radius, List<Entity> results);
    List<Entity> Query(Position center, int radius);
}

/// <summary>
/// Adapter que implementa o ISpatialIndex usando a biblioteca Splitice/QuadTrees.
/// Inclui object pooling para reduzir alocações em queries de alta frequência.
/// </summary>
public class QuadTreeSpatial(int minX, int minY, int width, int height)
    : ISpatialIndex
{
    private class QuadTreeItem(Entity entity, Position pos) : IRectQuadStorable
    {
        public Entity Entity { get; } = entity;
        public Rectangle Rect { get; set; } = new(pos.X, pos.Y, 1, 1); // Assumindo tamanho 1x1
    }
    
    private readonly QuadTreeRect<QuadTreeItem> _qtree = new(new Rectangle(minX, minY, width, height));
    private readonly Dictionary<Entity, QuadTreeItem> _items = new();
    
    // Object pool para reduzir alocações em queries
    private static readonly ConcurrentQueue<List<Entity>> EntityListPool = new();
    private static readonly ConcurrentQueue<List<QuadTreeItem>> ItemListPool = new();

    public void Add(Entity entity, Position position)
    {
        if (_items.ContainsKey(entity)) return;
        var item = new QuadTreeItem(entity, position);
        _items[entity] = item;
        _qtree.Add(item);
    }

    public void Remove(Entity entity)
    {
        if (_items.Remove(entity, out var item))
            _qtree.Remove(item);
    }

    public void Update(Entity entity, Position newPosition)
    {
        if (_items.TryGetValue(entity, out var item))
        {
            // A forma mais segura de atualizar é remover e adicionar novamente
            _qtree.Remove(item);
            item.Rect = new Rectangle(newPosition.X, newPosition.Y, 1, 1);
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
        if (EntityListPool.TryDequeue(out var list))
        {
            list.Clear();
            return list;
        }
        return new List<Entity>();
    }
    
    private static void ReturnPooledEntityList(List<Entity> list)
    {
        if (list != null && EntityListPool.Count < 20) // Limita o pool
        {
            list.Clear();
            EntityListPool.Enqueue(list);
        }
    }
    
    private static List<QuadTreeItem> GetPooledItemList()
    {
        if (ItemListPool.TryDequeue(out var list))
        {
            list.Clear();
            return list;
        }
        return new List<QuadTreeItem>();
    }
    
    private static void ReturnPooledItemList(List<QuadTreeItem> list)
    {
        if (list != null && ItemListPool.Count < 20) // Limita o pool
        {
            list.Clear();
            ItemListPool.Enqueue(list);
        }
    }
}