using Arch.Core;
using Arch.System;
using Game.DTOs.Npc;
using Game.DTOs.Player;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities;
using Game.ECS.Events;
using Game.ECS.Services;
using Game.ECS.Services.Map;
using Game.Network.Abstractions;
using Game.Server.Simulation.Systems;

namespace Game.Server.Simulation;

/// <summary>
/// Exemplo de uso do ECS como SERVIDOR.
/// O servidor executa a simulação completa com todos os sistemas.
/// </summary>
public sealed class ServerGameSimulation : GameSimulation
{
    private readonly INetworkManager _networkManager;
    private readonly ILoggerFactory? _loggerFactory;
    
    private readonly EntityIndex<int> _networkIndex = new();
    private readonly GameEventBus _eventBus = new();
    
    public ServerGameSimulation(
        INetworkManager network,
        WorldMapRegistry mapLoader,
        ILoggerFactory? factory = null)
        : base(factory?.CreateLogger<ServerGameSimulation>())
    {
        _networkManager = network;
        _loggerFactory = factory;
        
        foreach (var map in mapLoader.Maps)
        {            
            LogInformation("Registering map {MapId} - {MapName} ({Width}x{Height})", 
                map.Id, map.Name, map.Width, map.Height);
        }

        // Configure systems
        ConfigureSystems(World, Systems);

        // Initialize systems
        Systems.Initialize();
    }

    /// <summary>
    /// Configura todos os sistemas de servidor.
    /// Ordem importante: Input → AI → Movement → Combat → Projectile → Damage → Lifecycle → Regen → Sync
    /// </summary>
    protected override void ConfigureSystems(World world, Group<float> systems)
    {
        // ======== SISTEMAS DE JOGO ==========
        
        // 1. Input processa entrada do jogador
        // 2. NPC AI processa comportamento de NPCs
        // 3. Spatial sync garante ocupação inicial no grid
        // 4. Movement calcula novas posições
        // 5. Combat processa comandos de ataque
        // 6. Projectile move projéteis e aplica dano
        // 7. Damage processa dano periódico (DoT) e dano adiado
        //systems.Add(new DamageSystem(world, _loggerFactory?.CreateLogger<DamageSystem>()));
        // 8. Lifecycle processa spawn, morte e respawn de entidades
        // 9. Regeneration processa regeneração de vida/mana
        //systems.Add(new RegenerationSystem(world, _loggerFactory?.CreateLogger<RegenerationSystem>()));
        // 10. ServerSync envia atualizações para clientes
        systems.Add(new ServerSyncSystem(world, _networkManager, _eventBus, _loggerFactory?.CreateLogger<ServerSyncSystem>()));
    }
    
    public Entity CreatePlayer(ref PlayerSnapshot playerSnapshot)
    {
        var entity = World.CreatePlayer(ref playerSnapshot);
        _networkIndex.Register(playerSnapshot.NetworkId, entity);
        return entity;
    }
    
    public Entity CreateNpc(ref NpcData snapshot, ref Behaviour behaviour)
    {
        // Atualiza o template com a localização de spawn e networkId
        var entity = World.CreateNpc(ref snapshot, ref behaviour);
        _networkIndex.Register(snapshot.NetworkId, entity);
        return entity;
    }
    
    /// <summary>
    /// Tenta obter a entidade de um jogador pelo NetworkId.
    /// </summary>
    public bool TryGetEntity(int networkId, out Entity entity) =>
        _networkIndex.TryGetEntity(networkId, out entity);
    
    /// <summary>
    /// Destrói a entidade de um jogador pelo NetworkId.
    /// </summary>
    public bool DestroyEntity(int networkId)
    {
        if (!_networkIndex.TryGetEntity(networkId, out var entity))
            return false;
        
        _networkIndex.RemoveByKey(networkId);
        World.Destroy(entity);
        return true;
    }
    
    public bool ApplyPlayerInput(int networkId, Input data)
    {
        if (!TryGetEntity(networkId, out var entity))
            return false;
        ref var input = ref World.Get<Input>(entity);
        input.InputX = data.InputX;
        input.InputY = data.InputY;
        input.Flags = data.Flags;
        return true;
    }
}