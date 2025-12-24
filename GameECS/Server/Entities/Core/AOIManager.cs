using Arch.Core;
using GameECS.Shared.Entities.Components;
using GameECS.Shared.Navigation.Components;

namespace GameECS.Server.Entities.Core;

/// <summary>
/// Gerencia Area of Interest - determina visibilidade entre entidades.
/// </summary>
public sealed class AOIManager
{
    private readonly World _world;
    private readonly Dictionary<int, HashSet<int>> _visibleEntities = new();
    private readonly QueryDescription _entityQuery;

    public AOIManager(World world)
    {
        _world = world;
        _entityQuery = new QueryDescription()
            .WithAll<Identity, GridPosition>();
    }

    /// <summary>
    /// Atualiza as entidades visíveis para um observador.
    /// </summary>
    public AOIUpdateResult UpdateVisibility(
        Entity observer,
        in GridPosition position,
        in VisibilityConfig config)
    {
        var result = new AOIUpdateResult();
        int observerId = GetEntityId(observer);

        if (!_visibleEntities.TryGetValue(observerId, out var currentVisible))
        {
            currentVisible = new HashSet<int>();
            _visibleEntities[observerId] = currentVisible;
        }

        var newVisible = new HashSet<int>();
        var enteredView = new List<Entity>();

        // Copia valores para usar na lambda
        var observerPos = position;
        int viewRadius = config.ViewRadius;
        int maxEntities = config.MaxVisibleEntities;

        // Query entidades próximas
        _world.Query(in _entityQuery, (Entity entity, ref Identity identity, ref GridPosition entityPos) =>
        {
            if (entity.Equals(observer)) return;
            if (newVisible.Count >= maxEntities) return;

            int distance = observerPos.ManhattanDistanceTo(entityPos);

            if (distance <= viewRadius)
            {
                // Verifica invisibilidade
                if (_world.Has<Invisible>(entity) || _world.Has<Hidden>(entity))
                    return;

                newVisible.Add(identity.UniqueId);

                // É nova entidade na view?
                if (!currentVisible.Contains(identity.UniqueId))
                {
                    enteredView.Add(entity);
                }
            }
        });

        result.EnteredView = enteredView;

        // Detecta entidades que saíram da view
        foreach (int entityId in currentVisible)
        {
            if (!newVisible.Contains(entityId))
            {
                result.LeftViewIds.Add(entityId);
            }
        }

        // Atualiza cache
        _visibleEntities[observerId] = newVisible;
        result.VisibleCount = newVisible.Count;

        return result;
    }

    /// <summary>
    /// Obtém entidades visíveis por um observador.
    /// </summary>
    public IReadOnlySet<int> GetVisibleEntities(int observerId)
        => _visibleEntities.TryGetValue(observerId, out var visible)
            ? visible
            : new HashSet<int>();

    /// <summary>
    /// Verifica se uma entidade é visível para outra.
    /// </summary>
    public bool IsVisible(int observerId, int targetId)
        => _visibleEntities.TryGetValue(observerId, out var visible)
            && visible.Contains(targetId);

    /// <summary>
    /// Remove entidade do tracking.
    /// </summary>
    public void RemoveEntity(int entityId)
    {
        _visibleEntities.Remove(entityId);

        // Remove de todas as listas de visibilidade
        foreach (var visible in _visibleEntities.Values)
        {
            visible.Remove(entityId);
        }
    }

    private int GetEntityId(Entity entity)
    {
        if (_world.Has<Identity>(entity))
        {
            ref var identity = ref _world.Get<Identity>(entity);
            return identity.UniqueId;
        }
        return entity.Id;
    }
}

/// <summary>
/// Resultado de atualização de AOI.
/// </summary>
public struct AOIUpdateResult
{
    public List<Entity> EnteredView;
    public List<int> LeftViewIds;
    public int VisibleCount;

    public AOIUpdateResult()
    {
        EnteredView = new List<Entity>();
        LeftViewIds = new List<int>();
        VisibleCount = 0;
    }

    public readonly bool HasChanges => EnteredView.Count > 0 || LeftViewIds.Count > 0;
}
