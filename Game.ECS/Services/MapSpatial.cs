using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Services;

/// <summary>
/// Implementação otimizada de spatial hashing para IMapSpatial.
/// 
/// Otimizações:
/// - Pool de listas para evitar alocações em Remove
/// - CollectionsMarshal para acesso direto ao Dictionary
/// - SmallList inline para células com poucas entidades (comum)
/// - Query circular otimizada para percepção de NPCs
/// - Aggressive inlining em hot paths
/// </summary>
public sealed class MapSpatial : IMapSpatial
{
    /// <summary>
    /// Estrutura otimizada para armazenar entidades em uma célula.
    /// Usa inline storage para até 4 entidades (caso comum),
    /// fallback para List apenas quando necessário.
    /// </summary>
    private struct CellData
    {
        // Inline storage para até 4 entidades (32 bytes)
        private Entity _e0, _e1, _e2, _e3;
        private byte _inlineCount;
        
        // Overflow list (só aloca se > 4 entidades)
        private List<Entity>? _overflow;
        
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _overflow?.Count ?? _inlineCount;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Entity entity, Stack<List<Entity>> listPool)
        {
            if (_overflow != null)
            {
                _overflow.Add(entity);
                return;
            }
            
            switch (_inlineCount)
            {
                case 0: _e0 = entity; _inlineCount = 1; return;
                case 1: _e1 = entity; _inlineCount = 2; return;
                case 2: _e2 = entity; _inlineCount = 3; return;
                case 3: _e3 = entity; _inlineCount = 4; return;
                default:
                    // Overflow: move inline para list
                    _overflow = listPool.Count > 0 ? listPool.Pop() : new List<Entity>(8);
                    _overflow.Add(_e0);
                    _overflow.Add(_e1);
                    _overflow.Add(_e2);
                    _overflow.Add(_e3);
                    _overflow.Add(entity);
                    _inlineCount = 0;
                    break;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(Entity entity, Stack<List<Entity>> listPool)
        {
            if (_overflow != null)
            {
                bool removed = _overflow.Remove(entity);
                
                // Se ficou pequeno, volta para inline
                if (_overflow.Count <= 4)
                {
                    _inlineCount = (byte)_overflow.Count;
                    if (_inlineCount > 0) _e0 = _overflow[0];
                    if (_inlineCount > 1) _e1 = _overflow[1];
                    if (_inlineCount > 2) _e2 = _overflow[2];
                    if (_inlineCount > 3) _e3 = _overflow[3];
                    
                    _overflow.Clear();
                    listPool.Push(_overflow);
                    _overflow = null;
                }
                
                return removed;
            }
            
            // Busca linear no inline (máximo 4 comparações)
            for (int i = 0; i < _inlineCount; i++)
            {
                if (GetInline(i) == entity)
                {
                    RemoveAtInline(i);
                    return true;
                }
            }
            
            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly Entity GetInline(int index) => index switch
        {
            0 => _e0,
            1 => _e1,
            2 => _e2,
            _ => _e3
        };
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveAtInline(int index)
        {
            // Shift left
            switch (index)
            {
                case 0:
                    _e0 = _e1;
                    _e1 = _e2;
                    _e2 = _e3;
                    break;
                case 1:
                    _e1 = _e2;
                    _e2 = _e3;
                    break;
                case 2:
                    _e2 = _e3;
                    break;
            }
            _inlineCount--;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CopyTo(Span<Entity> results)
        {
            if (_overflow != null)
            {
                int toCopy = Math.Min(_overflow.Count, results.Length);
                for (int i = 0; i < toCopy; i++)
                    results[i] = _overflow[i];
                return toCopy;
            }
            
            int count = Math.Min(_inlineCount, results.Length);
            if (count > 0) results[0] = _e0;
            if (count > 1) results[1] = _e1;
            if (count > 2) results[2] = _e2;
            if (count > 3) results[3] = _e3;
            return count;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool ForEach(Func<Entity, bool> visitor)
        {
            if (_overflow != null)
            {
                foreach (var e in _overflow)
                    if (!visitor(e)) return false;
                return true;
            }
            
            if (_inlineCount > 0 && !visitor(_e0)) return false;
            if (_inlineCount > 1 && !visitor(_e1)) return false;
            if (_inlineCount > 2 && !visitor(_e2)) return false;
            if (_inlineCount > 3 && !visitor(_e3)) return false;
            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Entity GetFirst() => _overflow != null ? _overflow[0] : _e0;
        
        public void ReturnToPool(Stack<List<Entity>> listPool)
        {
            if (_overflow != null)
            {
                _overflow.Clear();
                listPool.Push(_overflow);
                _overflow = null;
            }
            _inlineCount = 0;
        }
    }
    
    private readonly Dictionary<SpatialPosition, CellData> _grid;
    private readonly Stack<List<Entity>> _listPool;
    
    public MapSpatial(int initialCapacity = 1024)
    {
        _grid = new Dictionary<SpatialPosition, CellData>(initialCapacity);
        _listPool = new Stack<List<Entity>>(32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(SpatialPosition position, in Entity entity)
    {
        ref var cell = ref CollectionsMarshal.GetValueRefOrAddDefault(_grid, position, out _);
        cell.Add(entity, _listPool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(SpatialPosition position, in Entity entity)
    {
        ref var cell = ref CollectionsMarshal.GetValueRefOrNullRef(_grid, position);
        if (Unsafe.IsNullRef(ref cell))
            return false;

        if (!cell.Remove(entity, _listPool))
            return false;

        if (cell.Count == 0)
        {
            cell.ReturnToPool(_listPool);
            _grid.Remove(position);
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Update(SpatialPosition oldPosition, SpatialPosition newPosition, in Entity entity)
    {
        if (!Remove(oldPosition, entity))
            return false;

        Insert(newPosition, entity);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryMove(SpatialPosition from, SpatialPosition to, in Entity entity)
    {
        if (HasOccupant(to))
            return false;

        return Update(from, to, entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int QueryAt(SpatialPosition position, Span<Entity> results)
    {
        ref var cell = ref CollectionsMarshal.GetValueRefOrNullRef(_grid, position);
        if (Unsafe.IsNullRef(ref cell))
            return 0;

        return cell.CopyTo(results);
    }

    public int QueryArea(SpatialPosition min, SpatialPosition max, Span<Entity> results)
    {
        int count = 0;
        int maxCount = results.Length;

        // Otimização: se área for pequena, itera diretamente
        int areaSize = (max.X - min.X + 1) * (max.Y - min.Y + 1) * (max.Floor - min.Floor + 1);
        
        if (areaSize <= _grid.Count)
        {
            // Área pequena: itera pelas posições
            for (sbyte z = min.Floor; z <= max.Floor && count < maxCount; z++)
            {
                for (int x = min.X; x <= max.X && count < maxCount; x++)
                {
                    for (int y = min.Y; y <= max.Y && count < maxCount; y++)
                    {
                        ref var cell = ref CollectionsMarshal.GetValueRefOrNullRef(
                            _grid, new SpatialPosition(x, y, z));
                        
                        if (!Unsafe.IsNullRef(ref cell))
                            count += cell.CopyTo(results[count..]);
                    }
                }
            }
        }
        else
        {
            // Área grande: itera pelo dictionary e filtra
            foreach (var kvp in _grid)
            {
                if (count >= maxCount) break;
                
                var pos = kvp.Key;
                if (pos.X >= min.X && pos.X <= max.X &&
                    pos.Y >= min.Y && pos.Y <= max.Y &&
                    pos.Floor >= min.Floor && pos.Floor <= max.Floor)
                {
                    count += kvp.Value.CopyTo(results[count..]);
                }
            }
        }

        return count;
    }
    
    /// <summary>
    /// Query otimizada para área circular (ideal para percepção de NPCs).
    /// Evita criar SpatialPosition para células fora do círculo.
    /// </summary>
    public int QueryCircle(SpatialPosition center, int radius, Span<Entity> results)
    {
        int count = 0;
        int maxCount = results.Length;
        int radiusSq = radius * radius;
        
        for (int dx = -radius; dx <= radius && count < maxCount; dx++)
        {
            int dxSq = dx * dx;
            int maxDy = (int)Math.Sqrt(radiusSq - dxSq);
            
            for (int dy = -maxDy; dy <= maxDy && count < maxCount; dy++)
            {
                var pos = new SpatialPosition(center.X + dx, center.Y + dy, center.Floor);
                
                ref var cell = ref CollectionsMarshal.GetValueRefOrNullRef(_grid, pos);
                if (!Unsafe.IsNullRef(ref cell))
                    count += cell.CopyTo(results[count..]);
            }
        }
        
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ForEachAt(SpatialPosition position, Func<Entity, bool> visitor)
    {
        ref var cell = ref CollectionsMarshal.GetValueRefOrNullRef(_grid, position);
        if (!Unsafe.IsNullRef(ref cell))
            cell.ForEach(visitor);
    }

    public void ForEachArea(SpatialPosition min, SpatialPosition max, Func<Entity, bool> visitor)
    {
        for (sbyte z = min.Floor; z <= max.Floor; z++)
        {
            for (int x = min.X; x <= max.X; x++)
            {
                for (int y = min.Y; y <= max.Y; y++)
                {
                    ref var cell = ref CollectionsMarshal.GetValueRefOrNullRef(
                        _grid, new SpatialPosition(x, y, z));
                    
                    if (!Unsafe.IsNullRef(ref cell))
                        if (!cell.ForEach(visitor))
                            return;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetFirstAt(SpatialPosition position, out Entity entity)
    {
        ref var cell = ref CollectionsMarshal.GetValueRefOrNullRef(_grid, position);
        if (!Unsafe.IsNullRef(ref cell) && cell.Count > 0)
        {
            entity = cell.GetFirst();
            return true;
        }

        entity = Entity.Null;
        return false;
    }

    public void Clear()
    {
        foreach (var kvp in _grid)
        {
            var cell = kvp.Value;
            cell.ReturnToPool(_listPool);
        }
        _grid.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasOccupant(SpatialPosition position)
    {
        ref var cell = ref CollectionsMarshal.GetValueRefOrNullRef(_grid, position);
        return !Unsafe.IsNullRef(ref cell) && cell.Count > 0;
    }
    
    /// <summary>
    /// Retorna estatísticas para debugging/profiling.
    /// </summary>
    public (int CellCount, int PooledLists, int TotalEntities) GetStats()
    {
        int totalEntities = 0;
        foreach (var kvp in _grid)
            totalEntities += kvp.Value.Count;
        
        return (_grid.Count, _listPool.Count, totalEntities);
    }
}