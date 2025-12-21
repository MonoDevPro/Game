using Arch.Core;
using Arch.System;
using Game.Domain.Entities;
using Game.ECS.Server.Modules.Navigation;
using Game.ECS.Server.Modules.Sync;
using Game.ECS.Shared;
using Game.ECS.Shared.Components.Navigation;
using Game.ECS.Shared.Core.Entities;
using Game.ECS.Shared.Data.Navigation;
using Game.ECS.Shared.Services.Entities;
using Game.ECS.Shared.Services.Network;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Server;

/// <summary>
/// Exemplo de uso do ECS como SERVIDOR.
/// O servidor executa a simulação completa com todos os sistemas.
/// </summary>
public sealed class ServerGameSimulation : GameSimulation
{
    private readonly INetworkManager _networkManager;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly EntityRegistry _entityRegistry;
    
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
        _entityRegistry = new EntityRegistry(World);
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
        
        // 10. ServerSync envia atualizações para clientes
        systems.Add(new ServerSyncSystem(world, _networkManager, _loggerFactory?.CreateLogger<ServerSyncSystem>()));
    }
    
    public Entity CreatePlayerEntity(ref PlayerData playerSnapshot)
    {
        var entity = _navigationModule?.CreateAgent(new GridPosition(playerSnapshot.X, playerSnapshot.Y))
            ?? throw new InvalidOperationException("Navigation module is not initialized.");
        _entityRegistry.Register(entity);
        return entity;
    }
    
    public Entity CreateNpcEntity(ref NpcData npcSnapshot)
    {
        var entity = _navigationModule?.CreateAgent(new GridPosition(npcSnapshot.X, npcSnapshot.Y))
            ?? throw new InvalidOperationException("Navigation module is not initialized.");
        _entityRegistry.Register(entity);
        return entity;
    }
    
    public bool ApplyPlayerMoveInput(Entity entity, MoveInput data)
    {
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