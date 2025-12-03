using Arch.Core;
using Arch.System;
using Game.Domain.Entities;
using Game.ECS;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.ECS.Schema.Components;
using Game.ECS.Schema.Templates;
using Game.ECS.Services;
using Game.ECS.Services.Map;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Game.Server.ECS.Systems;
using Game.Server.Npc;

namespace Game.Server.ECS;

/// <summary>
/// Exemplo de uso do ECS como SERVIDOR.
/// O servidor executa a simulação completa com todos os sistemas.
/// </summary>
public sealed class ServerGameSimulation : GameSimulation
{
    private readonly INetworkManager _networkManager;
    private readonly INpcRepository _npcRepository;
    
    // Index para busca rápida de entidades por NetworkId
    private readonly EntityIndex<int> _playerIndex = new();
    private readonly EntityIndex<int> _npcIndex = new();
    
    public ServerGameSimulation(
        INetworkManager network, 
        IEnumerable<Map> maps,
        INpcRepository npcRepository,
        ILoggerFactory? factory = null)
        : base(factory?.CreateLogger<ServerGameSimulation>())
    {
        _networkManager = network;
        _npcRepository = npcRepository;
        
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
        systems.Add(new NpcAISystem(world, mapService!, loggerFactory?.CreateLogger<NpcAISystem>()));
        
        // 3. Movement calcula novas posições
        systems.Add(new MovementSystem(world, mapService!));
        
        // 4. Combat processa comandos de ataque
        systems.Add(new CombatSystem(world, mapService!, loggerFactory?.CreateLogger<CombatSystem>()));
        
        // 5. Projectile move projéteis e aplica dano
        systems.Add(new ProjectileSystem(world, mapService!, loggerFactory?.CreateLogger<ProjectileSystem>()));
        
        // 6. Damage processa dano periódico (DoT) e dano adiado
        systems.Add(new DamageSystem(world, loggerFactory?.CreateLogger<DamageSystem>()));
        
        // 7. Lifecycle processa spawn, morte e respawn de entidades
        systems.Add(new LifecycleSystem(world, loggerFactory?.CreateLogger<LifecycleSystem>()));
        
        // 8. Regeneration processa regeneração de vida/mana
        systems.Add(new RegenerationSystem(world, loggerFactory?.CreateLogger<RegenerationSystem>()));
        
        // 9. ServerSync envia atualizações para clientes
        systems.Add(new ServerSyncSystem(world, _networkManager, loggerFactory?.CreateLogger<ServerSyncSystem>()));
    }

    #region Player Management
    
    /// <summary>
    /// Cria um jogador a partir de um template.
    /// </summary>
    public Entity CreatePlayer(PlayerTemplate template)
    {
        var entity = World.CreatePlayer(Strings, template);
        _playerIndex.Register(template.IdentityTemplate.NetworkId, entity);
        return entity;
    }
    
    /// <summary>
    /// Tenta obter a entidade de um jogador pelo NetworkId.
    /// </summary>
    public bool TryGetPlayerEntity(int networkId, out Entity entity) =>
        _playerIndex.TryGetEntity(networkId, out entity);
    
    /// <summary>
    /// Destrói a entidade de um jogador pelo NetworkId.
    /// </summary>
    public bool DestroyEntity(int networkId)
    {
        if (!_playerIndex.TryGetEntity(networkId, out var entity))
            return false;
        
        _playerIndex.RemoveByKey(networkId);
        World.Destroy(entity);
        return true;
    }
    
    #endregion
    
    #region NPC Management
    
    /// <summary>
    /// Cria um NPC a partir de um template na posição especificada.
    /// </summary>
    public Entity CreateNpcFromTemplate(NpcTemplate template, int x, int y, sbyte floor, int mapId, int networkId)
    {
        // Atualiza o template com a localização de spawn e networkId
        var spawnTemplate = new NpcTemplate
        {
            Id = template.Id,
            IdentityTemplate = template.IdentityTemplate with { NetworkId = networkId },
            LocationTemplate = new LocationTemplate(mapId, floor, x, y),
            DirectionTemplate = template.DirectionTemplate,
            VitalsTemplate = template.VitalsTemplate,
            StatsTemplate = template.StatsTemplate,
            BehaviorTemplate = template.BehaviorTemplate
        };
        
        var entity = World.CreateNpc(spawnTemplate, Strings);
        _npcIndex.Register(networkId, entity);
        return entity;
    }
    
    /// <summary>
    /// Tenta obter a entidade de um NPC pelo NetworkId.
    /// </summary>
    public bool TryGetNpcEntity(int networkId, out Entity entity) =>
        _npcIndex.TryGetEntity(networkId, out entity);
    
    /// <summary>
    /// Destrói a entidade de um NPC pelo NetworkId.
    /// </summary>
    public bool DestroyNpc(int networkId)
    {
        if (!_npcIndex.TryGetEntity(networkId, out var entity))
            return false;
        
        _npcIndex.RemoveByKey(networkId);
        World.Destroy(entity);
        return true;
    }
    
    #endregion

    public bool ApplyPlayerInput(Entity e, Input data)
    {
        ref var input = ref World.Get<Input>(e);
        input.InputX = data.InputX;
        input.InputY = data.InputY;
        input.Flags = data.Flags;
        return true;
    }
}