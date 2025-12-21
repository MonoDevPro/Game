using Arch.Core;
using GameECS.Modules.Entities.Shared.Components;
using GameECS.Modules.Entities.Shared.Data;
using GameECS.Modules.Navigation.Shared.Components;

namespace GameECS.Modules.Entities.Client;

/// <summary>
/// Módulo de entidades client-side.
/// Gerencia representação visual e estado local de entidades.
/// </summary>
public sealed class ClientEntitiesModule : IDisposable
{
    private readonly World _world;
    private readonly Dictionary<int, Entity> _entityIdMap = new();

    public ClientEntitiesModule(World world)
    {
        _world = world;
    }

    /// <summary>
    /// Cria entidade local a partir de dados do servidor.
    /// </summary>
    public Entity CreateEntityFromServer(
        int uniqueId,
        EntityType type,
        string name,
        int level,
        int x,
        int y)
    {
        var entity = _world.Create(
            new EntityIdentity
            {
                UniqueId = uniqueId,
                Type = type,
                TemplateId = 0
            },
            EntityName.Create(name),
            new EntityLevel(level),
            new GridPosition(x, y)
        );

        _entityIdMap[uniqueId] = entity;
        return entity;
    }

    /// <summary>
    /// Atualiza posição de entidade.
    /// </summary>
    public bool UpdatePosition(int uniqueId, int x, int y)
    {
        if (!_entityIdMap.TryGetValue(uniqueId, out var entity))
            return false;

        if (!_world.IsAlive(entity))
        {
            _entityIdMap.Remove(uniqueId);
            return false;
        }

        ref var pos = ref _world.Get<GridPosition>(entity);
        pos.X = x;
        pos.Y = y;
        return true;
    }

    /// <summary>
    /// Remove entidade (saiu da view).
    /// </summary>
    public bool RemoveEntity(int uniqueId)
    {
        if (!_entityIdMap.TryGetValue(uniqueId, out var entity))
            return false;

        if (_world.IsAlive(entity))
            _world.Destroy(entity);

        _entityIdMap.Remove(uniqueId);
        return true;
    }

    /// <summary>
    /// Obtém entidade por ID único.
    /// </summary>
    public Entity? GetEntity(int uniqueId)
        => _entityIdMap.TryGetValue(uniqueId, out var entity) && _world.IsAlive(entity)
            ? entity
            : null;

    /// <summary>
    /// Verifica se entidade existe localmente.
    /// </summary>
    public bool HasEntity(int uniqueId)
        => _entityIdMap.TryGetValue(uniqueId, out var entity) && _world.IsAlive(entity);

    /// <summary>
    /// Quantidade de entidades visíveis.
    /// </summary>
    public int EntityCount => _entityIdMap.Count;

    /// <summary>
    /// Atualiza estado (limpa entidades mortas).
    /// </summary>
    public void Update()
    {
        var toRemove = new List<int>();
        foreach (var (id, entity) in _entityIdMap)
        {
            if (!_world.IsAlive(entity))
                toRemove.Add(id);
        }
        foreach (var id in toRemove)
            _entityIdMap.Remove(id);
    }

    public void Dispose()
    {
        // Destroy all tracked entities
        foreach (var entity in _entityIdMap.Values)
        {
            if (_world.IsAlive(entity))
                _world.Destroy(entity);
        }
        _entityIdMap.Clear();
    }
}
