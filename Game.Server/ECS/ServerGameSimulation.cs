using Arch.Core;
using Arch.System;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;
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
    
    public ServerGameSimulation(INetworkManager network, ILoggerFactory factory, IMapService? mapService = null)
    {
        _networkManager = network;
        _loggerFactory = factory;
        MapService = mapService ?? new MapService();
        
        // Configura os sistemas
        ConfigureSystems(World, Systems);
    }

    /// <summary>
    /// Configura todos os sistemas de servidor.
    /// Ordem importante: Input → Movement → Combat → Sync
    /// </summary>
    protected override void ConfigureSystems(World world, Group<float> systems)
    {
        // Sistemas de entrada (input não vem do servidor, vem do cliente)
        // Mas o servidor valida e aplica
        systems.Add(new InputSystem(world));
        
        // Sistema de dano diferido (aplica dano no momento correto da animação)
        systems.Add(new DeferredDamageSystem(world, _loggerFactory.CreateLogger<DeferredDamageSystem>()));
        
        // Sistemas de movimento
        systems.Add(new MovementSystem(world, MapService!));
        
        // Sistemas de combate
        systems.Add(new AttackSystem(world, MapService!, _loggerFactory.CreateLogger<AttackSystem>()));
        
        // Sistemas de saúde
        systems.Add(new HealthSystem(world));
        
        // Sistemas de revive
        systems.Add(new ReviveSystem(World, _loggerFactory.CreateLogger<ReviveSystem>()));
        
        // Sistemas de IA
        systems.Add(new AISystem(world, MapService!));
        
        // Sistemas de atualização de entidades
        systems.Add(new ServerSyncSystem(world, _networkManager, _loggerFactory.CreateLogger<ServerSyncSystem>()));
        
    }

    public Entity CreatePlayer(in PlayerData data)
    {
        var entity = World.CreatePlayer(PlayerIndex, data);
        RegisterSpatial(entity);
        return entity;
    }
    
    public bool DestroyPlayer(Entity entity)
    {
        PlayerIndex.RemoveByEntity(entity);
        UnregisterSpatial(entity);
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