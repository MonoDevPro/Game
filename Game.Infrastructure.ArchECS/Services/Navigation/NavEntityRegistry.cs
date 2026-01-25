using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Arch.Core;

namespace Game.Infrastructure.ArchECS.Services.Navigation;

/// <summary>
/// Registro de entidades de navegação.
/// Mapeia IDs externos (CharacterId, NpcId, etc) para Entity do ECS.
/// Thread-safe para registro/desregistro, lookup otimizado para leitura.
/// </summary>
public sealed class NavEntityRegistry
{
    private readonly ConcurrentDictionary<int, Entity> _entityById = new();
    private readonly ConcurrentDictionary<Entity, int> _idByEntity = new();

    /// <summary>
    /// Registra uma entidade com um ID único.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Register(int id, Entity entity)
    {
        _entityById[id] = entity;
        _idByEntity[entity] = id;
    }

    /// <summary>
    /// Remove registro de uma entidade por ID.
    /// </summary>
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

    /// <summary>
    /// Remove registro de uma entidade.
    /// </summary>
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

    /// <summary>
    /// Tenta obter Entity por ID.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetEntity(int id, out Entity entity)
        => _entityById.TryGetValue(id, out entity);

    /// <summary>
    /// Tenta obter ID por Entity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetId(Entity entity, out int id)
        => _idByEntity.TryGetValue(entity, out id);

    /// <summary>
    /// Obtém Entity por ID. Lança exceção se não encontrado.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity GetEntity(int id)
    {
        if (_entityById.TryGetValue(id, out var entity))
            return entity;
        throw new KeyNotFoundException($"Entity with ID {id} not found in NavEntityRegistry");
    }

    /// <summary>
    /// Obtém ID por Entity. Lança exceção se não encontrado.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetId(Entity entity)
    {
        if (_idByEntity.TryGetValue(entity, out var id))
            return id;
        throw new KeyNotFoundException($"Entity {entity} not found in NavEntityRegistry");
    }

    /// <summary>
    /// Verifica se ID está registrado.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(int id) => _entityById.ContainsKey(id);

    /// <summary>
    /// Verifica se Entity está registrada.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Entity entity) => _idByEntity.ContainsKey(entity);

    /// <summary>
    /// Número de entidades registradas.
    /// </summary>
    public int Count => _entityById.Count;

    /// <summary>
    /// Limpa todos os registros.
    /// </summary>
    public void Clear()
    {
        _entityById.Clear();
        _idByEntity.Clear();
    }

    /// <summary>
    /// Enumera todos os IDs registrados.
    /// </summary>
    public IEnumerable<int> GetAllIds() => _entityById.Keys;

    /// <summary>
    /// Enumera todas as entidades registradas.
    /// </summary>
    public IEnumerable<Entity> GetAllEntities() => _entityById.Values;
}
