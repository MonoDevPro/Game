using Arch.Core;
using Arch.System;
using Game.Contracts;
using Game.Infrastructure.ArchECS.Services.Entities.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Map;

namespace Game.Infrastructure.ArchECS.Services.Entities;

public class EntityModule : IDisposable
{
    private readonly CentralEntityRegistry _registry;
    private readonly World _world;
    private readonly WorldMap _worldMap;
    private readonly Group<long> _systems;
    private bool _disposed;

    private static readonly EntityDomain PlayerDomain = 
        EntityDomain.Combat | 
        EntityDomain.Navigation | 
        EntityDomain.Inventory | 
        EntityDomain.Quest | 
        EntityDomain.Social | 
        EntityDomain.Environment | 
        EntityDomain.Effects | 
        EntityDomain.Network;

    // Domínio individual usado para lookups exatos (TryGetEntity, GetExternalId).
    // PlayerDomain é flag combinada e não existe como chave no registry.
    private const EntityDomain PlayerLookupDomain = EntityDomain.Navigation;

    public EntityModule(World world, WorldMap worldMap, int initialCapacity = 256)
    {
        _registry = new CentralEntityRegistry(initialCapacity);
        _world = world;
        _worldMap = worldMap;
        _systems = new Group<long>(
            "EntityRegistry"
            // Aqui você pode adicionar sistemas relacionados a entidades, se necessário.
        );
    }

    public void Tick(long serverTick)
    {
        _systems.BeforeUpdate(in serverTick);
        _systems.Update(in serverTick);
        _systems.AfterUpdate(in serverTick);
    }

    public int GetPlayerEntityCount()
    {
        var count = 0;
        foreach (var _ in _registry.GetEntitiesByDomain(PlayerDomain))
            count++;
        return count;
    }

    public int GetPlayerEntityCharacterId(Entity entity)
    {
        var externalId = _registry.GetExternalId(entity, PlayerLookupDomain);
        return externalId;
    }

    public bool TryGetPlayerEntityCharacterId(Entity entity, out int characterId)
    {
        return _registry.TryGetExternalId(entity, PlayerLookupDomain, out characterId);
    }

    public Entity CreatePlayerEntity(int characterId, string name)
    {
        if (TryGetEntityByCharacterId(characterId, out var entity))
            return entity;

        entity = _world.Create();

        _registry.RegisterMultiDomain(characterId, name, entity, PlayerDomain);

        return entity;
    }

    public bool TryGetEntityMetadataByCharacterId(int characterId, out EntityMetadata meta)
    {
        meta = default;

        if (!TryGetEntityByCharacterId(characterId, out var entity))
            return false;

        meta = _registry.GetMetadata(entity);
        return true;
    }

    public void DestroyPlayerEntity(int characterId)
    {

        if (!TryGetEntityByCharacterId(characterId, out var entity))
            return;

        _registry.Unregister(entity);
        _world.Destroy(entity);
    }

    public IEnumerable<EntityMetadata> GetAllPlayerEntities()
    {
        foreach (var entity in _registry.GetEntitiesByDomain(PlayerDomain))
            yield return _registry.GetMetadata(entity);
    }

    private bool TryGetEntityByCharacterId(int characterId, out Entity entity)
    {
        entity = default;
        return _registry.TryGetEntity(characterId, PlayerLookupDomain, out entity);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _systems.Dispose();
        _registry.Dispose();

    }
}