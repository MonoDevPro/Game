using System.Runtime.CompilerServices;
using Arch.Bus;
using Arch.Core;
using Arch.LowLevel;
using Game.Infrastructure.ArchECS.Services.EntityRegistry.Events;

namespace Game.Infrastructure.ArchECS.Services.EntityRegistry;

/// <summary>
/// Registro centralizado de entidades usando Arch.LowLevel para máxima performance.
/// Utiliza Resources para gerenciar registros por domínio com zero-allocation.
/// </summary>
public sealed class CentralEntityRegistry : IDisposable
{
    // Resources por domínio (cache-friendly, zero-allocation após warm-up)
    private readonly Resources<DomainRegistryData> _domainRegistries;
    
    // Handles por domínio (O(1) lookup)
    private readonly Dictionary<EntityDomain, Handle<DomainRegistryData>> _domainHandles;
    
    // Metadata global (Entity -> Metadata)
    private readonly Dictionary<Entity, EntityMetadata> _entityMetadata;
    
    // Reverse lookup (ExternalId+Domain -> Entity)
    private readonly Dictionary<(int externalId, EntityDomain domain), Entity> _entityLookup;

    public CentralEntityRegistry(int initialCapacity = 1024)
    {
        _domainRegistries = new Resources<DomainRegistryData>(capacity: 16); // Poucos domínios
        _domainHandles = new Dictionary<EntityDomain, Handle<DomainRegistryData>>(16);
        _entityMetadata = new Dictionary<Entity, EntityMetadata>(initialCapacity);
        _entityLookup = new Dictionary<(int, EntityDomain), Entity>(initialCapacity);
        
        // Pre-registrar domínios comuns
        InitializeDomain(EntityDomain.Combat);
        InitializeDomain(EntityDomain.Navigation);
        InitializeDomain(EntityDomain.AI);
    }

    #region Initialization

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InitializeDomain(EntityDomain domain)
    {
        if (_domainHandles.ContainsKey(domain))
            return;

        var data = new DomainRegistryData
        {
            Domain = domain,
            EntityById = new Dictionary<int, Entity>(256),
            IdByEntity = new Dictionary<Entity, int>(256)
        };

        var handle = _domainRegistries.Add(data);
        _domainHandles[domain] = handle;
    }

    #endregion

    #region Registration

    /// <summary>
    /// Registra uma entidade em um domínio específico.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Register(int externalId, Entity entity, EntityDomain domain)
    {
        InitializeDomain(domain);
        
        var handle = _domainHandles[domain];
        ref var registryData = ref _domainRegistries.Get(handle);
        
        registryData.EntityById[externalId] = entity;
        registryData.IdByEntity[entity] = externalId;
        
        _entityLookup[(externalId, domain)] = entity;
        
        if (!_entityMetadata.ContainsKey(entity))
        {
            _entityMetadata[entity] = new EntityMetadata
            {
                ExternalId = externalId,
                Entity = entity,
                Domain = domain,
                RegisteredAt = DateTime.UtcNow
            };
        }
        else
        {
            // Adicionar domínio adicional
            ref var metadata = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrNullRef(_entityMetadata, entity);
            metadata.Domain |= domain;
        }
        
        var evt = new EntityRegisteredEvent(entity, _entityMetadata[entity]);
        EventBus.Send(ref evt);
    }

    /// <summary>
    /// Registra uma entidade em múltiplos domínios (ex: Combat + Navigation).
    /// </summary>
    public void RegisterMultiDomain(int externalId, Entity entity, EntityDomain domains)
    {
        foreach (EntityDomain domain in Enum.GetValues(typeof(EntityDomain)))
        {
            if (domains.HasFlag(domain) && domain != EntityDomain.None)
            {
                InitializeDomain(domain);
                
                var handle = _domainHandles[domain];
                ref var registryData = ref _domainRegistries.Get(handle);
                
                registryData.EntityById[externalId] = entity;
                registryData.IdByEntity[entity] = externalId;
                
                _entityLookup[(externalId, domain)] = entity;
            }
        }
        
        var meta = new EntityMetadata
        {
            ExternalId = externalId,
            Entity = entity,
            Domain = domains,
            RegisteredAt = DateTime.UtcNow
        };

        _entityMetadata[entity] = meta;
        
        var evt = new EntityRegisteredEvent(entity, meta);
        EventBus.Send(ref evt);
    }

    /// <summary>
    /// Remove registro de uma entidade de todos os domínios.
    /// </summary>
    public bool Unregister(Entity entity)
    {
        if (!_entityMetadata.TryGetValue(entity, out var metadata))
            return false;

        foreach (EntityDomain domain in Enum.GetValues<EntityDomain>())
        {
            if (metadata.Domain.HasFlag(domain) && domain != EntityDomain.None)
            {
                if (_domainHandles.TryGetValue(domain, out var handle))
                {
                    ref var registryData = ref _domainRegistries.Get(handle);
                    
                    if (registryData.IdByEntity.TryGetValue(entity, out var externalId))
                    {
                        registryData.EntityById.Remove(externalId);
                        registryData.IdByEntity.Remove(entity);
                        _entityLookup.Remove((externalId, domain));
                    }
                }
            }
        }

        _entityMetadata.Remove(entity);
        
        var evt = new EntityUnregisteredEvent(entity, metadata);
        EventBus.Send(ref evt);
        
        return true;
    }

    #endregion

    #region Queries

    /// <summary>
    /// Obtém Entity por ID externo em um domínio específico.
    /// Hot path - AggressiveInlining.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity GetEntity(int externalId, EntityDomain domain)
    {
        if (!_domainHandles.TryGetValue(domain, out var handle))
            throw new KeyNotFoundException($"Domain {domain} not initialized");

        ref var registryData = ref _domainRegistries.Get(handle);
        
        if (registryData.EntityById.TryGetValue(externalId, out var entity))
            return entity;

        throw new KeyNotFoundException($"Entity with ID {externalId} not found in domain {domain}");
    }

    /// <summary>
    /// Tenta obter Entity por ID externo (zero-allocation).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetEntity(int externalId, EntityDomain domain, out Entity entity)
    {
        entity = default;
        
        if (!_domainHandles.TryGetValue(domain, out var handle))
            return false;

        ref var registryData = ref _domainRegistries.Get(handle);
        return registryData.EntityById.TryGetValue(externalId, out entity);
    }

    /// <summary>
    /// Obtém ID externo de uma entidade.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetExternalId(Entity entity, EntityDomain domain)
    {
        if (!_domainHandles.TryGetValue(domain, out var handle))
            throw new KeyNotFoundException($"Domain {domain} not initialized");

        ref var registryData = ref _domainRegistries.Get(handle);
        
        if (registryData.IdByEntity.TryGetValue(entity, out var id))
            return id;

        throw new KeyNotFoundException($"Entity {entity} not found in domain {domain}");
    }

    /// <summary>
    /// Obtém metadata completo de uma entidade.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityMetadata GetMetadata(Entity entity)
    {
        if (_entityMetadata.TryGetValue(entity, out var metadata))
            return metadata;
        
        throw new KeyNotFoundException($"Entity {entity} not found in registry");
    }

    /// <summary>
    /// Verifica se entidade está registrada.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Entity entity) => _entityMetadata.ContainsKey(entity);

    /// <summary>
    /// Conta entidades em um domínio.
    /// </summary>
    public int CountByDomain(EntityDomain domain)
    {
        if (!_domainHandles.TryGetValue(domain, out var handle))
            return 0;

        ref var registryData = ref _domainRegistries.Get(handle);
        return registryData.EntityById.Count;
    }

    /// <summary>
    /// Total de entidades registradas.
    /// </summary>
    public int TotalCount => _entityMetadata.Count;

    /// <summary>
    /// Obtém todas as entidades de um domínio.
    /// </summary>
    public IEnumerable<Entity> GetEntitiesByDomain(EntityDomain domain)
    {
        if (domain == EntityDomain.None)
            return Enumerable.Empty<Entity>();

        // Fast path: domínio exato já registrado (sem alocação extra).
        if (_domainHandles.TryGetValue(domain, out var handle))
        {
            ref var registryData = ref _domainRegistries.Get(handle);
            return registryData.EntityById.Values;
        }

        // Fallback: consulta por combinação de flags (ex: Combat | Navigation).
        var merged = new HashSet<Entity>();
        foreach (var (registeredDomain, registeredHandle) in _domainHandles)
        {
            if (!domain.HasFlag(registeredDomain))
                continue;

            ref var data = ref _domainRegistries.Get(registeredHandle);
            foreach (var entity in data.EntityById.Values)
            {
                merged.Add(entity);
            }
        }

        return merged;
    }

    /// <summary>
    /// Obtém estatísticas do registry.
    /// </summary>
    public RegistryStatistics GetStatistics()
    {
        var stats = new RegistryStatistics
        {
            TotalEntities = _entityMetadata.Count,
            DomainCounts = new Dictionary<EntityDomain, int>()
        };

        foreach (var kvp in _domainHandles)
        {
            ref var registryData = ref _domainRegistries.Get(kvp.Value);
            stats.DomainCounts[kvp.Key] = registryData.EntityById.Count;
        }

        return stats;
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Limpa todos os registros.
    /// </summary>
    public void Clear()
    {
        foreach (var handle in _domainHandles.Values)
        {
            ref var registryData = ref _domainRegistries.Get(handle);
            registryData.EntityById.Clear();
            registryData.IdByEntity.Clear();
        }
        
        _entityMetadata.Clear();
        _entityLookup.Clear();
    }

    public void Dispose()
    {
        Clear();
        _domainRegistries.Dispose();
    }

    #endregion
}

/// <summary>
/// Dados internos de um registro de domínio.
/// Struct para cache-locality usando Arch.LowLevel Resources.
/// </summary>
internal struct DomainRegistryData
{
    public EntityDomain Domain;
    public Dictionary<int, Entity> EntityById;
    public Dictionary<Entity, int> IdByEntity;
}
