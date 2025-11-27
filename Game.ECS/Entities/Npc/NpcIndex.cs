using Arch.Core;
using Game.ECS.Services;

namespace Game.ECS.Entities.Npc;

public interface INpcIndex
{
    bool TryGetNpcEntity(int networkId, out Entity entity);
    bool TryGetNetworkId(Entity entity, out int networkId);
}

/// <summary>
/// Índice bidirecional para entidades de NPC, baseado no NetworkId.
/// </summary>
public sealed class NpcIndex : EntityIndex<int>, INpcIndex
{
    public bool TryGetNpcEntity(int networkId, out Entity entity) => TryGetEntity(networkId, out entity);
    public bool TryGetNetworkId(Entity entity, out int networkId) => TryGetKeyByEntityId(entity.Id, out networkId);
}