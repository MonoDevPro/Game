using Arch.Core;
using Arch.System;
using Game.Domain.Entities;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Services;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Game.Server.ECS.Systems;

namespace Game.Server.ECS;

/// <summary>
/// Exemplo de uso do ECS como SERVIDOR.
/// O servidor executa a simulação completa com todos os sistemas.
/// </summary>
public sealed class ServerGameSimulation : GameSimulation
{
    private readonly INetworkManager _networkManager;
    private readonly ILoggerFactory _loggerFactory;
    
    public ServerGameSimulation(
        INetworkManager network, 
        ILoggerFactory factory, 
        IEnumerable<Map> maps) : base(mapService: new MapService())
    {
        _networkManager = network;
        _loggerFactory = factory;
        
        // Registra os mapas fornecidoss
        foreach (var map in maps)
        {
            MapService?.RegisterMap(
                map.Id,
                new MapGrid(map.Width, map.Height, map.Layers, map.GetCollisionGrid()),
                new MapSpatial()
            );
        }
        
        // Configura os sistemas
        ConfigureSystems(World, Systems);
    }

    /// <summary>
    /// Configura todos os sistemas de servidor.
    /// Ordem importante: Input → Movement → Combat → Sync
    /// </summary>
    protected override void ConfigureSystems(World world, Group<float> systems)
    {
        if (MapService == null)
            throw new InvalidOperationException("MapService não pode ser nulo na simulação do servidor.");
        
        // ⭐ Ordem importante:
        // 0. NPC AI processa percepção, estado e decisões antes dos inputs
        systems.Add(new NpcPerceptionSystem(world, MapService, _loggerFactory.CreateLogger<NpcPerceptionSystem>()));
        systems.Add(new NpcAISystem(world, _loggerFactory.CreateLogger<NpcAISystem>()));
        
        // 0.5 NPC Pathfinding calcula caminhos A* (ANTES do NpcMovementSystem)
        systems.Add(new NpcPathfindingSystem(world, MapService, _loggerFactory.CreateLogger<NpcPathfindingSystem>()));
        
        // 0.6 NPC Movement usa waypoints do pathfinding para gerar inputs
        systems.Add(new NpcMovementSystem(world, _loggerFactory.CreateLogger<NpcMovementSystem>()));
        systems.Add(new NpcCombatSystem(world, _loggerFactory.CreateLogger<NpcCombatSystem>()));

        // 1. Input processa entrada do jogador
        systems.Add(new InputSystem(world));
        
        // 2. Movement calcula novas posições
        systems.Add(new MovementSystem(world, MapService));
        
        // 4. Combat processa estado de combate
        systems.Add(new CombatSystem(world, _loggerFactory.CreateLogger<CombatSystem>()));
        
        // 4.1 Damage processa dano periódico (DoT), dano adiado e cria projéteis
        systems.Add(new DamageSystem(world, MapService, _loggerFactory.CreateLogger<DamageSystem>()));
        
        // 4.2 Projectile processa movimento e colisão de projéteis
        systems.Add(new ProjectileSystem(world, MapService, _loggerFactory.CreateLogger<ProjectileSystem>()));
        
        // 4.3 Death processa morte de entidades
        systems.Add(new DeathSystem(world, _loggerFactory.CreateLogger<DeathSystem>()));
        
        // 5. Regeneration processa vida/mana/dano
        systems.Add(new RegenerationSystem(world, _loggerFactory.CreateLogger<RegenerationSystem>()));
        
        // 6. Revive processa ressurreição
        systems.Add(new ReviveSystem(World, _loggerFactory.CreateLogger<ReviveSystem>()));
        
        // 8. ⭐ SpatialSync sincroniza mudanças de posição com o índice espacial
        //    (DEVE rodar ANTES do ServerSyncSystem para garantir que queries espaciais funcionem)
        systems.Add(new SpatialSyncSystem(World, MapService, _loggerFactory.CreateLogger<SpatialSyncSystem>()));
        
        // 9. ServerSync envia atualizações para clientes
        systems.Add(new ServerSyncSystem(world, _networkManager, _loggerFactory.CreateLogger<ServerSyncSystem>()));
    }

    public bool ApplyPlayerInput(Entity e, Input data)
    {
        ref var input = ref World.Get<Input>(e);
        input.InputX = data.InputX;
        input.InputY = data.InputY;
        input.Flags = data.Flags;
        return true;
    }
}