using Arch.Core;
using Arch.System;
using Game.Domain.Entities;
using Game.DTOs.Game.Player;
using Game.ECS;
using Game.ECS.Navigation.Client;
using Game.ECS.Navigation.Server;
using Game.ECS.Navigation.Shared.Components;
using Game.ECS.Navigation.Shared.Data;
using Game.ECS.Systems;
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
    private readonly ServerNavigationModule? _navigationModule;

    public override void Update(in float deltaTime)
    {
        base.Update(in deltaTime);
        
        // Atualiza o módulo de navegação do servidor
        _navigationModule?.Tick(serverTick: Environment.TickCount);
    }


    public ServerGameSimulation(
        INetworkManager network, 
        IEnumerable<Map> maps,
        ILoggerFactory? factory = null)
        : base(factory?.CreateLogger<ServerGameSimulation>())
    {
        _networkManager = network;
        _loggerFactory = factory;
        _navigationModule = new ServerNavigationModule(
            world: World,
            width: 100,   // Exemplo, deve ser baseado no mapa real
            height: 100   // Exemplo, deve ser baseado no mapa real
        );
        
        // Registra os mapas fornecidos
        foreach (var map in maps)
        {
            // TODO: armazenar mapas em um gerenciador de mapas
        }
        
        // Configura os sistemas
        ConfigureSystems(World, Systems);
        
        // Inicializa os sistemas
        Systems.Initialize();
    }

    /// <summary>
    /// Configura todos os sistemas de servidor.
    /// Ordem importante: Input → AI → Movement → Combat → Projectile → Damage → Lifecycle → Regen → Sync
    /// </summary>
    protected override void ConfigureSystems(World world, Group<float> systems)
    {
        // ======== SISTEMAS DE JOGO ==========
        
        // 0. NetworkEntity gerencia IDs de rede
        systems.Add(new NetworkEntitySystem(world, _loggerFactory?.CreateLogger<NetworkEntitySystem>()));
        
        // 1. Input processa entrada do jogador
        systems.Add(new InputSystem(world));
        
        // 2. NPC AI processa comportamento de NPCs
        systems.Add(new AISystem(world, _loggerFactory?.CreateLogger<AISystem>()));
        
        // 5. Combat processa comandos de ataque
        systems.Add(new CombatSystem(world, _loggerFactory?.CreateLogger<CombatSystem>()));
        
        // 7. Damage processa dano periódico (DoT) e dano adiado
        systems.Add(new DamageSystem(world, _loggerFactory?.CreateLogger<DamageSystem>()));
        
        // 8. Lifecycle processa spawn, morte e respawn de entidades
        systems.Add(new LifecycleSystem(world, _loggerFactory?.CreateLogger<LifecycleSystem>()));
        
        // 9. Regeneration processa regeneração de vida/mana
        systems.Add(new RegenerationSystem(world, _loggerFactory?.CreateLogger<RegenerationSystem>()));
        
        // 10. ServerSync envia atualizações para clientes
        systems.Add(new ServerSyncSystem(world, _networkManager, EventBus, _loggerFactory?.CreateLogger<ServerSyncSystem>()));
    }
    
    public Entity CreatePlayerEntity(ref PlayerData playerSnapshot)
    {
        return _navigationModule?.CreateAgent(new GridPosition(playerSnapshot.X, playerSnapshot.Y))
            ?? throw new InvalidOperationException("Navigation module is not initialized.");
    }
    
    public bool ApplyPlayerInput(int networkId, MoveInput data)
    {
        if (!TryGetPlayerEntity(networkId, out var entity))
            return false;
        
        World.Add<PathRequest>(entity, new PathRequest
        {
            TargetX = data.TargetX,
            TargetY = data.TargetY,
            Flags = PathRequestFlags.None,
            Priority = PathPriority.Normal
        });
        return true;
    }
}