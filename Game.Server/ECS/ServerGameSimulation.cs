using Arch.Core;
using Arch.System;
using Game.Domain.Entities;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Data;
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
    // 0. NPC AI processa percepção, estado e decisões antes dos inputs
    systems.Add(new NpcPerceptionSystem(world, MapService, _loggerFactory.CreateLogger<NpcPerceptionSystem>()));
    systems.Add(new NpcAISystem(world, _loggerFactory.CreateLogger<NpcAISystem>()));
    systems.Add(new NpcMovementSystem(world));
    systems.Add(new NpcCombatSystem(world));

    // 1. Input processa entrada do jogador
    systems.Add(new InputSystem(world));
        
        // 2. Movement calcula novas posições
        systems.Add(new MovementSystem(world, MapService));
        
        // 3. Attack processa ataques
        systems.Add(new AttackSystem(world, MapService, _loggerFactory.CreateLogger<AttackSystem>()));
        
        // 4. Combat processa estado de combate
        systems.Add(new CombatSystem(world));
        
        // 5. Vitals processa vida/mana/dano
        systems.Add(new VitalsSystem(world, _loggerFactory.CreateLogger<VitalsSystem>()));
        
        // 6. Revive processa ressurreição
        systems.Add(new ReviveSystem(World, _loggerFactory.CreateLogger<ReviveSystem>()));
        
        // 8. ⭐ SpatialSync sincroniza mudanças de posição com o índice espacial
        //    (DEVE rodar ANTES do ServerSyncSystem para garantir que queries espaciais funcionem)
        systems.Add(new SpatialSyncSystem(World, MapService, _loggerFactory.CreateLogger<SpatialSyncSystem>()));
        
        // 9. ServerSync envia atualizações para clientes
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

    public Entity CreateNpc(in NPCData data)
    {
        var entity = World.CreateNPC(data);
        NpcIndex.AddMapping(data.NetworkId, entity);
        Systems.Get<SpatialSyncSystem>()
            .OnEntityCreated(entity, 
                new Position
                {
                    X = data.PositionX, 
                    Y = data.PositionY, 
                    Z = data.PositionZ
                }, data.MapId);
        return entity;
    }
    
    public bool DestroyPlayer(Entity entity)
    {
        // Remove do spatial
        if (World.TryGet(entity, out MapId mapId) && 
            World.TryGet(entity, out Position position))
            Systems.Get<SpatialSyncSystem>()
                .OnEntityDestroyed(entity, 
                    position, 
                    mapId.Value);
    
        PlayerIndex.RemoveByEntity(entity);
        World.Destroy(entity);
        return true;
    }

    public bool DestroyNpc(int networkId)
    {
        if (!NpcIndex.TryGetEntity(networkId, out var entity))
            return false;

        if (World.TryGet(entity, out MapId mapId) &&
            World.TryGet(entity, out Position position))
        {
            Systems.Get<SpatialSyncSystem>()
                .OnEntityDestroyed(entity, position, mapId.Value);
        }

        NpcIndex.RemoveByKey(networkId);
        World.Destroy(entity);
        return true;
    }

    public bool TryGetNpcEntity(int networkId, out Entity entity) =>
        NpcIndex.TryGetEntity(networkId, out entity);

    public bool ApplyPlayerInput(Entity e, Input data)
    {
        ref var input = ref World.Get<Input>(e);
        input.InputX = data.InputX;
        input.InputY = data.InputY;
        input.Flags = data.Flags;
        return true;
    }
}