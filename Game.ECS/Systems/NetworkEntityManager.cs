using Arch.Core;
using Game.ECS.Entities.Components;
using Game.ECS.Schema.Components;
using Game.ECS.Services;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

public sealed partial class NetworkEntitySystem(World world, ILogger<NetworkEntitySystem>? logger = null) 
    : GameSystem(world)
{
    private readonly EntityIndex<int> _networkIndex = new();
    
    /// <summary>
    /// Tries to get any entity (player or NPC) by network ID.
    /// </summary>
    public bool TryGetEntity(int networkId, out Entity entity) => 
        _networkIndex.TryGetEntity(networkId, out entity);
    
    // Registro de nova entidade
    public void Register(Entity entity, int networkId)
    {
        _networkIndex.Register(networkId, entity);
        World.AddOrGet<NetworkId>(entity).Value = networkId;
    }

    // Desregistro de entidade
    public void Unregister(Entity entity)
    {
        if (!World.Has<NetworkId>(entity))
            return;
        var netId = World.Get<NetworkId>(entity).Value;
        _networkIndex.RemoveByKey(netId);
        World.Remove<NetworkId>(entity);
    }
    
    public int GetNetworkId(Entity entity)
    {
        if (!World.Has<NetworkId>(entity))
            throw new InvalidOperationException($"Entity {entity} is not registered in NetworkEntitySystem.");
        return World.Get<NetworkId>(entity).Value;
    }
    
}