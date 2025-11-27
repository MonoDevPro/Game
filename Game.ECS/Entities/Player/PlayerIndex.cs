using Arch.Core;
using Game.ECS.Services;

namespace Game.ECS.Entities.Player;

public interface IPlayerIndex
{
    bool TryGetPlayerEntity(int networkId, out Entity entity);
    bool TryGetNetworkId(Entity entity, out int networkId);
}

public sealed class PlayerIndex : EntityIndex<int>, IPlayerIndex
{
    public bool TryGetPlayerEntity(int networkId, out Entity entity) => TryGetEntity(networkId, out entity);
    public bool TryGetNetworkId(Entity entity, out int networkId) => TryGetKeyByEntityId(entity.Id, out networkId);
}