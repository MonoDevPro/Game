using Arch.Core;
using Arch.System;
using Game.Domain.Entities;
using Game.ECS;
using Game.ECS.Schema.Components;
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
    
    public ServerGameSimulation(
        INetworkManager network, 
        IEnumerable<Map> maps,
        INpcRepository npcRepository,
        ILoggerFactory? factory = null)
        : base(factory?.CreateLogger<ServerGameSimulation>())
    {
        _networkManager = network;
        _npcRepository = npcRepository;
        
        // Registra os mapas fornecidoss
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
    /// Ordem importante: Input → Movement → Combat → Sync
    /// </summary>
    protected override void ConfigureSystems(World world, Group<float> systems, ILoggerFactory? loggerFactory = null)
    {
        var mapService = MapIndex;
        // ⭐ Ordem importante:
        
        // ======== SISTEMAS DE JOGO ==========
        
        //0. NetworkEntity gerencia IDs de rede
        systems.Add(new NetworkEntitySystem(world, loggerFactory?.CreateLogger<NetworkEntitySystem>()));
        
        // 1. Input processa entrada do jogador
        systems.Add(new InputSystem(world));
        
        // 2. Movement calcula novas posições
        systems.Add(new MovementSystem(world, mapService!));
        
        // 4.1 Damage processa dano periódico (DoT), dano adiado e cria projéteis
        systems.Add(new DamageSystem(world, loggerFactory?.CreateLogger<DamageSystem>()));
        
        // 4.3 Death processa morte de entidades
        systems.Add(new LifecycleSystem(world, loggerFactory?.CreateLogger<LifecycleSystem>()));
        
        // 5. Regeneration processa vida/mana/dano
        systems.Add(new RegenerationSystem(world, loggerFactory?.CreateLogger<RegenerationSystem>()));
        
        // 9. ServerSync envia atualizações para clientes
        systems.Add(new ServerSyncSystem(world, _networkManager, loggerFactory?.CreateLogger<ServerSyncSystem>()));
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