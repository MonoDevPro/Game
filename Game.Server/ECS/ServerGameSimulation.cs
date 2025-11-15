using Arch.Core;
using Arch.System;
using Game.Domain.Entities;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;
using Game.ECS.Extensions;
using Game.ECS.Services;
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
        // 1. Input processa entrada do jogador
        systems.Add(new InputSystem(world));
        
        // 2. Movement calcula novas posições
        systems.Add(new MovementSystem(world, MapService));
        
        // 3. Attack processa ataques
        systems.Add(new AttackSystem(world, MapService, _loggerFactory.CreateLogger<AttackSystem>()));
        
        // 4. Vitals processa vida/mana/dano
        systems.Add(new VitalsSystem(world, _loggerFactory.CreateLogger<VitalsSystem>()));
        
        // 5. Revive processa ressurreição
        systems.Add(new ReviveSystem(World, _loggerFactory.CreateLogger<ReviveSystem>()));
        
        // 6. ⭐ SpatialSync sincroniza mudanças de posição com o índice espacial
        //    (DEVE rodar ANTES do ServerSyncSystem para garantir que queries espaciais funcionem)
        systems.Add(new SpatialSyncSystem(World, MapService, _loggerFactory.CreateLogger<SpatialSyncSystem>()));
        
        // 7. ServerSync envia atualizações para clientes
        systems.Add(new ServerSyncSystem(world, _networkManager, _loggerFactory.CreateLogger<ServerSyncSystem>()));
    }

    public Entity CreatePlayer(in PlayerData data)
    {
        var entity = World.CreatePlayer(PlayerIndex, data);
        
        // Adiciona ao spatial
        if (World.TryGet(entity, out MapId mapId) && 
            World.TryGet(entity, out Position position))
            Systems.Get<SpatialSyncSystem>()
                .OnEntityCreated(entity, position, mapId.Value);
        
        return entity;
    }
    
    public bool DestroyPlayer(Entity entity)
    {
        // Remove do spatial
        if (World.TryGet(entity, out MapId mapId) && 
            World.TryGet(entity, out Position position))
            Systems.Get<SpatialSyncSystem>()
                .OnEntityDestroyed(entity, position, mapId.Value);
    
        PlayerIndex.RemoveByEntity(entity);
        World.Destroy(entity);
        return true;
    }

    public bool ApplyPlayerInput(Entity e, PlayerInput data)
    {
        ref var input = ref World.Get<PlayerInput>(e);
        input.InputX = data.InputX;
        input.InputY = data.InputY;
        input.Flags = data.Flags;
        return true;
    }
}