using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Services;
using Game.ECS.Services.Index;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

public sealed partial class NetworkEntitySystem(World world, ILogger<NetworkEntitySystem>? logger = null) 
    : GameSystem(world)
{
    private readonly EntityIndex<int> _networkIndex = new();
    private readonly IdGenerator _idGenerator = new();

    [Query]
    [Any<UniqueID>]
    [None<NetworkId>]
    private void NetworkEntityRegister(in Entity entity)
    {
        Register(entity);
        
        logger?.LogInformation("Registered network entity {EntityId} with NetworkId {NetworkId}", 
            entity.Id, World.Get<NetworkId>(entity).Value);
    }
    
    /// <summary>
    /// Tries to get any entity (player or NPC) by network ID.
    /// </summary>
    public bool TryGetEntity(int networkId, out Entity entity) => 
        _networkIndex.TryGetEntity(networkId, out entity);
    
    // Registro de nova entidade
    public void Register(Entity entity)
    {
        // 1. Gera um novo ID
        int netId = _idGenerator.Next();
        
        // 2. Registra no índice bidirecional que você já possui
        _networkIndex.Register(netId, entity);
        
        // 3. Adiciona o componente NetworkId à entidade no Arch ECS
        World.AddOrGet<NetworkId>(entity).Value = netId;
    }

    // Desregistro de entidade
    public void Unregister(Entity entity)
    {
        // 1. Verifica se a entidade possui um NetworkId.
        if (!World.Has<NetworkId>(entity))
            return;
        
        // 2. Obtém o ID da entidade.
        var netId = World.Get<NetworkId>(entity).Value;
        
        // 3. Remove do índice bidirecional e recicla o ID.
        _networkIndex.RemoveByKey(netId);
        _idGenerator.Recycle(netId);
        
        // 4. Remove o componente NetworkId da entidade no Arch ECS.
        World.Remove<NetworkId>(entity);
    }
}