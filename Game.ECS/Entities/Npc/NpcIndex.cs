using Arch.Core;
using Game.ECS.Services;

namespace Game.ECS.Entities.Npc;

public interface INpcIndex
{
    bool TryGetNpcEntity(int networkId, out Entity entity);
    bool TryGetNetworkId(Entity entity, out int networkId);
    bool TryGetEntity(int networkId, out Entity entity);
    void Register(int networkId, Entity entity);
    void Unregister(int networkId);
}

/// <summary>
/// Índice bidirecional para entidades de NPC, baseado no NetworkId.
/// </summary>
public sealed class NpcIndex : EntityIndex<int>, INpcIndex
{
    public bool TryGetNpcEntity(int networkId, out Entity entity) => TryGetEntity(networkId, out entity);
    public bool TryGetNetworkId(Entity entity, out int networkId) => TryGetKeyByEntityId(entity.Id, out networkId);
    public void Register(int networkId, Entity entity) => AddMapping(networkId, entity);
    public void Unregister(int networkId) => RemoveByKey(networkId);
}