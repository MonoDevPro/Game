using Arch.Core;
using Game.ECS.Services;

namespace Game.ECS.Entities.Player;

public interface IPlayerIndex
{
    bool TryGetPlayerEntity(int networkId, out Entity entity);
    bool TryGetNetworkId(Entity entity, out int networkId);
    bool TryGetEntity(int networkId, out Entity entity);
    void Register(int networkId, Entity entity);
    void Unregister(int networkId);
}

public sealed class PlayerIndex : EntityIndex<int>, IPlayerIndex
{
    public bool TryGetPlayerEntity(int networkId, out Entity entity) => TryGetEntity(networkId, out entity);
    public bool TryGetNetworkId(Entity entity, out int networkId) => TryGetKeyByEntityId(entity.Id, out networkId);
    public void Register(int networkId, Entity entity) => AddMapping(networkId, entity);
    public void Unregister(int networkId) => RemoveByKey(networkId);
}