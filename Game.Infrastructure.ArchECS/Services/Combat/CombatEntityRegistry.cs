using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Arch.Core;

namespace Game.Infrastructure.ArchECS.Services.Combat;

/// <summary>
/// Registro de entidades de combate.
/// Mapeia IDs externos (CharacterId, NpcId, etc) para Entity do ECS.
/// </summary>
public sealed class CombatEntityRegistry
{
    private readonly ConcurrentDictionary<int, Entity> _entityById = new();
    private readonly ConcurrentDictionary<Entity, int> _idByEntity = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Register(int id, Entity entity)
    {
        _entityById[id] = entity;
        _idByEntity[entity] = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Unregister(int id)
    {
        if (_entityById.TryRemove(id, out var entity))
        {
            _idByEntity.TryRemove(entity, out _);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Unregister(Entity entity)
    {
        if (_idByEntity.TryRemove(entity, out var id))
        {
            _entityById.TryRemove(id, out _);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetEntity(int id, out Entity entity)
        => _entityById.TryGetValue(id, out entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetId(Entity entity, out int id)
        => _idByEntity.TryGetValue(entity, out id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(int id) => _entityById.ContainsKey(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Entity entity) => _idByEntity.ContainsKey(entity);

    public int Count => _entityById.Count;

    public void Clear()
    {
        _entityById.Clear();
        _idByEntity.Clear();
    }
}
