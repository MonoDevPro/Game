using Arch.Core;
using Arch.System;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Events;
using Game.ECS.Services;
using Game.ECS.Services.Map;
using Game.ECS.Services.Navigation;
using Game.ECS.Services.Snapshot.Data;
using Game.ECS.Services.Snapshot.Sync;
using Game.ECS.Services.Snapshot.Systems;

namespace Game.Server.Simulation;

/// <summary>
/// Exemplo de uso do ECS como SERVIDOR.
/// O servidor executa a simulação completa com todos os sistemas.
/// </summary>
public sealed class ServerGameSimulation : GameSimulation
{
    private readonly INetSync _networkManager;
    private readonly ILoggerFactory? _loggerFactory;

    private readonly EntityIndex<int> _networkIndex = new();
    private readonly GameEventBus _eventBus = new();
    
    // Módulos de navegação por mapa
    private readonly Dictionary<int, NavigationModule> _navigationModules = new();
    
    // Tick counter para navegação (usa long para tick-based)
    private long _serverTick;
    
    public ServerGameSimulation(
        INetSync network,
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
            
            // Cria módulo de navegação para cada mapa
            var navModule = new NavigationModule(World, map);
            _navigationModules[map.Id] = navModule;
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
        // 10. PlayerNavigation sync para broadcast de movimento jogadores
        systems.Add(new PlayerNavigationSyncSystem(world, _networkManager, _navigationModules, _loggerFactory?.CreateLogger<PlayerNavigationSyncSystem>()));
        // 11. NpcNavigation sync para broadcast de movimento NPCs
        systems.Add(new NpcNavigationSyncSystem(world, _networkManager, _navigationModules, _loggerFactory?.CreateLogger<NpcNavigationSyncSystem>()));
    }
    
    public Entity CreatePlayer(ref PlayerData template)
    {
        var entity = World.Create(
            new NetworkId { Value = template.NetworkId },
            new PlayerControlled { },
            new UniqueID { Value = template.PlayerId },
            new GenderId { Value = template.Gender },
            new VocationId { Value = template.Vocation },
            new Direction { X = template.DirX, Y = template.DirY },
            new CombatStats
            {
                AttackPower = template.PhysicalAttack,
                MagicPower = template.MagicAttack,
                Defense = template.PhysicalDefense,
                MagicDefense = template.MagicDefense,
                AttackRange = 1.5f,
                AttackSpeed = 1f
            },
            new CombatState { CooldownTimer = 0f },
            new Health { Current = template.Hp, Max = template.MaxHp, RegenerationRate = template.HpRegen },
            new Mana { Current = template.Mp, Max = template.MaxMp, RegenerationRate = template.MpRegen },
            new SpawnPoint(template.MapId, template.X, template.Y, template.Z));
        
        _networkIndex.Register(template.NetworkId, entity);
        
        // Adiciona componentes de navegação se houver módulo de navegação para o mapa
        if (_navigationModules.TryGetValue(template.MapId, out var navModule))
        {
            navModule.AddNavigationComponents(entity, new Position
            {
                X = template.X, 
                Y = template.Y, 
                Z = template.Z
            });
        }
        return entity;
    }
    
    public Entity CreateNpc(ref NpcData template)
    {
        var entity = World.Create(
            new NetworkId { Value = template.NetworkId },
            new AIControlled { },
            new UniqueID { Value = template.NpcId }, // Using NetworkId as UniqueId for server-side
            new AIBehaviour
            {
                Type = template.BehaviorType,
                VisionRange = template.VisionRange,
                AttackRange = template.AttackRange,
                LeashRange = template.LeashRange,
                PatrolRadius = template.PatrolRadius,
                IdleDurationMin = template.IdleDurationMin,
                IdleDurationMax = template.IdleDurationMax
            },
            new Direction { X = template.DirX, Y = template.DirY },
            new CombatStats
            {
                AttackPower = template.PhysicalAttack,
                MagicPower = template.MagicAttack,
                Defense = template.PhysicalDefense,
                MagicDefense = template.MagicDefense,
                AttackRange = template.AttackRange,
                AttackSpeed = 1f
            },
            new CombatState { CooldownTimer = 0f },
            new Health { Current = template.Hp, Max = template.MaxHp, RegenerationRate = template.HpRegen },
            new Mana { Current = template.Mp, Max = template.MaxMp, RegenerationRate = template.MpRegen },
            new SpawnPoint(template.MapId, template.X, template.Y, template.Z));
        
        _networkIndex.Register(template.NetworkId, entity);
        
        // Adiciona componentes de navegação se houver módulo de navegação para o mapa
        if (_navigationModules.TryGetValue(template.MapId, out var navModule))
        {
            navModule.AddNavigationComponents(entity, new Position
            {
                X = template.X, 
                Y = template.Y, 
                Z = template.Z
            });
        }
        return entity;
    }
    
    /// <summary>
    /// Solicita movimento de um NPC para uma posição de destino.
    /// </summary>
    public bool RequestNpcMove(int networkId, int targetX, int targetY, int targetZ = 0)
    {
        if (!TryGetEntity(networkId, out var entity))
            return false;
        
        ref var mapId = ref World.Get<MapId>(entity);
        if (!_navigationModules.TryGetValue(mapId.Value, out var navModule))
            return false;
            
        navModule.RequestMove(entity, targetX, targetY, targetZ);
        return true;
    }
    
    /// <summary>
    /// Para o movimento de um NPC.
    /// </summary>
    public bool StopNpcMove(int networkId)
    {
        if (!TryGetEntity(networkId, out var entity))
            return false;
            
        ref var mapId = ref World.Get<MapId>(entity);
        if (!_navigationModules.TryGetValue(mapId.Value, out var navModule))
            return false;
            
        navModule.StopMovement(entity);
        return true;
    }
    
    /// <summary>
    /// Obtém o módulo de navegação para um mapa específico.
    /// </summary>
    public NavigationModule? GetNavigationModule(int mapId)
    {
        return _navigationModules.TryGetValue(mapId, out var navModule) ? navModule : null;
    }
    
    /// <summary>
    /// Tick de navegação - chamado internamente pelo Update.
    /// </summary>
    private void TickNavigation()
    {
        _serverTick++;
        foreach (var navModule in _navigationModules.Values)
        {
            navModule.Tick(_serverTick);
        }
    }
    
    /// <summary>
    /// Override do Update para também processar navegação.
    /// </summary>
    public override void Update(in float deltaTime)
    {
        // Processa navegação (tick-based)
        TickNavigation();
        
        // Processa sistemas ECS
        base.Update(in deltaTime);
    }
    
    /// <summary>
    /// Disposes all resources including navigation modules.
    /// </summary>
    public override void Dispose()
    {
        foreach (var navModule in _navigationModules.Values)
        {
            navModule.Dispose();
        }
        _navigationModules.Clear();
        
        base.Dispose();
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
    
    public bool ApplyPlayerInput(int networkId, PlayerInputRequest inputRequest)
    {
        if (!TryGetEntity(networkId, out var entity))
            return false;
        
        // Se tiver input de movimento, solicita movimento via navegação
        if (inputRequest.InputX != 0 || inputRequest.InputY != 0)
        {
            ref var mapId = ref World.Get<MapId>(entity);
            ref var position = ref World.Get<Position>(entity);
            
            // Calcula posição alvo baseada no input direcional
            int targetX = position.X + inputRequest.InputX;
            int targetY = position.Y + inputRequest.InputY;
            
            // Solicita movimento direto (um passo apenas, sem pathfinding completo)
            if (_navigationModules.TryGetValue(mapId.Value, out var navModule))
                navModule.RequestMove(entity, targetX, targetY, position.Z);
        }
        
        return true;
    }
    
    /// <summary>
    /// Solicita movimento de um jogador para uma posição específica (click-to-move).
    /// </summary>
    public bool RequestPlayerMove(int networkId, int targetX, int targetY, int targetZ = 0)
    {
        if (!TryGetEntity(networkId, out var entity))
            return false;
            
        ref var mapId = ref World.Get<MapId>(entity);
        if (!_navigationModules.TryGetValue(mapId.Value, out var navModule))
            return false;
            
        navModule.RequestMove(entity, targetX, targetY, targetZ);
        return true;
    }
    
    /// <summary>
    /// Para o movimento de um jogador.
    /// </summary>
    public bool StopPlayerMove(int networkId)
    {
        if (!TryGetEntity(networkId, out var entity))
            return false;
            
        ref var mapId = ref World.Get<MapId>(entity);
        if (!_navigationModules.TryGetValue(mapId.Value, out var navModule))
            return false;
            
        navModule.StopMovement(entity);
        return true;
    }
}