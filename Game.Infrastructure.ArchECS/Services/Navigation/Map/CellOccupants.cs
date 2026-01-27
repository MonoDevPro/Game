using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arch.Core;

namespace Game.Infrastructure.ArchECS.Services.Navigation.Map;

/// <summary>
/// Armazena entidades em uma célula com inline storage para até 4 entidades.
/// Evita alocações no caso comum (1-4 entidades por célula).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CellOccupants
{
    private Entity _e0, _e1, _e2, _e3;
    private byte _inlineCount;
    private List<Entity>? _overflow;
    
    public readonly int Count
    {
        [MethodImpl(MethodImplOptions. AggressiveInlining)]
        get => _overflow?.Count ?? _inlineCount;
    }
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public void Add(Entity entity, Stack<List<Entity>> pool)
    {
        if (_overflow != null)
        {
            _overflow.Add(entity);
            return;
        }
        
        switch (_inlineCount)
        {
            case 0:  _e0 = entity; _inlineCount = 1; return;
            case 1: _e1 = entity; _inlineCount = 2; return;
            case 2: _e2 = entity; _inlineCount = 3; return;
            case 3: _e3 = entity; _inlineCount = 4; return;
            default: 
                _overflow = pool. Count > 0 ? pool.Pop() : new List<Entity>(8);
                _overflow.Add(_e0);
                _overflow.Add(_e1);
                _overflow.Add(_e2);
                _overflow.Add(_e3);
                _overflow.Add(entity);
                _inlineCount = 0;
                break;
        }
    }
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public bool Remove(Entity entity, Stack<List<Entity>> pool)
    {
        if (_overflow != null)
        {
            bool removed = _overflow. Remove(entity);
            if (_overflow.Count <= 4)
            {
                _inlineCount = (byte)_overflow.Count;
                if (_inlineCount > 0) _e0 = _overflow[0];
                if (_inlineCount > 1) _e1 = _overflow[1];
                if (_inlineCount > 2) _e2 = _overflow[2];
                if (_inlineCount > 3) _e3 = _overflow[3];
                _overflow.Clear();
                pool.Push(_overflow);
                _overflow = null;
            }
            return removed;
        }
        
        for (int i = 0; i < _inlineCount; i++)
        {
            if (GetInline(i) == entity)
            {
                RemoveAt(i);
                return true;
            }
        }
        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly Entity GetInline(int i) => i switch
    {
        0 => _e0, 1 => _e1, 2 => _e2, _ => _e3
    };
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveAt(int i)
    {
        switch (i)
        {
            case 0: _e0 = _e1; _e1 = _e2; _e2 = _e3; break;
            case 1: _e1 = _e2; _e2 = _e3; break;
            case 2: _e2 = _e3; break;
        }
        _inlineCount--;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int CopyTo(Span<Entity> buffer)
    {
        if (_overflow != null)
        {
            int n = Math.Min(_overflow.Count, buffer. Length);
            for (int i = 0; i < n; i++) buffer[i] = _overflow[i];
            return n;
        }
        
        int count = Math.Min(_inlineCount, buffer.Length);
        if (count > 0) buffer[0] = _e0;
        if (count > 1) buffer[1] = _e1;
        if (count > 2) buffer[2] = _e2;
        if (count > 3) buffer[3] = _e3;
        return count;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Entity GetFirst() => _overflow != null ? _overflow[0] : _e0;
}