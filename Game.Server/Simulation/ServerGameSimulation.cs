using Arch.Core;
using Arch.System;
using Game.Domain.Entities;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Services.Map;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Game.Server.ECS.Systems;

namespace Game.Server.Simulation;

/// <summary>
/// Exemplo de uso do ECS como SERVIDOR.
/// O servidor executa a simulação completa com todos os sistemas.
/// </summary>
public sealed class ServerGameSimulation : GameSimulation
{
    private readonly INetworkManager _networkManager;
    
    public ServerGameSimulation(
        INetworkManager network, 
        IEnumerable<Map> maps,
        ILoggerFactory? factory = null)
        : base(factory?.CreateLogger<ServerGameSimulation>())
    {
        _networkManager = network;
        
        // Registra os mapas fornecidos
        foreach (var map in maps)
        {
            MapIndex.RegisterMap(
                map.Id,
                new MapGrid(map.Width, map.Height, map.Layers, map.GetCollisionGrid()),
                new MapSpatial()
            );
        }
        
        // Configura os sistemas
        ConfigureSystems(World, Systems, factory);
        
        // Inicializa os sistemas
        Systems.Initialize();
    }

    /// <summary>
    /// Configura todos os sistemas de servidor.
    /// Ordem importante: Input → AI → Movement → Combat → Projectile → Damage → Lifecycle → Regen → Sync
    /// </summary>
    protected override void ConfigureSystems(World world, Group<float> systems, ILoggerFactory? loggerFactory = null)
    {
        var mapService = MapIndex;
        
        // ======== SISTEMAS DE JOGO ==========
        
        // 0. NetworkEntity gerencia IDs de rede
        systems.Add(new NetworkEntitySystem(world, loggerFactory?.CreateLogger<NetworkEntitySystem>()));
        
        // 1. Input processa entrada do jogador
        systems.Add(new InputSystem(world));
        
        // 2. NPC AI processa comportamento de NPCs
        systems.Add(new NpcAISystem(world, mapService, loggerFactory?.CreateLogger<NpcAISystem>()));
        
        // 3. Spatial sync garante ocupação inicial no grid
        systems.Add(new SpatialSyncSystem(world, mapService, loggerFactory?.CreateLogger<SpatialSyncSystem>()));
        
        // 4. Movement calcula novas posições
        systems.Add(new MovementSystem(world, mapService, EventBus, loggerFactory?.CreateLogger<MovementSystem>()));
        
        // 5. Combat processa comandos de ataque
        systems.Add(new CombatSystem(world, mapService, loggerFactory?.CreateLogger<CombatSystem>()));
        
        // 6. Projectile move projéteis e aplica dano
        systems.Add(new ProjectileSystem(world, mapService, loggerFactory?.CreateLogger<ProjectileSystem>()));
        
        // 7. Damage processa dano periódico (DoT) e dano adiado
        systems.Add(new DamageSystem(world, loggerFactory?.CreateLogger<DamageSystem>()));
        
        // 8. Lifecycle processa spawn, morte e respawn de entidades
        systems.Add(new LifecycleSystem(world, loggerFactory?.CreateLogger<LifecycleSystem>()));
        
        // 9. Regeneration processa regeneração de vida/mana
        systems.Add(new RegenerationSystem(world, loggerFactory?.CreateLogger<RegenerationSystem>()));
        
        // 10. ServerSync envia atualizações para clientes
        systems.Add(new ServerSyncSystem(world, _networkManager, EventBus, loggerFactory?.CreateLogger<ServerSyncSystem>()));
    }
    
    public bool ApplyPlayerInput(int networkId, Input data)
    {
        if (!TryGetPlayerEntity(networkId, out var entity))
            return false;
        ref var input = ref World.Get<Input>(entity);
        input.InputX = data.InputX;
        input.InputY = data.InputY;
        input.Flags = data.Flags;
        return true;
    }
}