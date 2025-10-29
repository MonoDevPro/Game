using Game.ECS.Components;
using Game.ECS.Systems;
using Arch.Core;
using Arch.System;
using Game.ECS.Entities.Factories;
using Game.ECS.Services;

namespace Game.ECS.Examples;

/// <summary>
/// Exemplo de uso do ECS como CLIENTE.
/// O cliente executa uma simulação local parcial: apenas movimento local, renderização e input.
/// Estado autorizado vem do servidor.
/// </summary>
public sealed class ClientGameSimulation : GameSimulation
{
    private MovementSystem _movementSystem = null!;
    private InputSystem _inputSystem = null!;
    
    private Entity _localPlayer;
    private int _localNetworkId;
    
    public ClientGameSimulation(GameEventSystem? gameEvents = null, IMapService? mapService = null)
        : base(gameEvents ?? new GameEventSystem(),
            mapService ?? new MapService())
    {
        ConfigureSystems(World, GameEvents, Systems);
    }

    /// <summary>
    /// Configura sistemas apenas para o cliente.
    /// Ordem: Input → Movement (previsão local) → Sync (recebe correções do servidor)
    /// </summary>
    protected override void ConfigureSystems(World world, GameEventSystem eventSystem, Group<float> systems)
    {
        // Sistemas de entrada do jogador
        _inputSystem = new InputSystem(world, eventSystem);
        systems.Add(_inputSystem);
        
        // Sistemas de movimento (previsão local)
        _movementSystem = new MovementSystem(world, eventSystem);
        systems.Add(_movementSystem);
    }
    
    public Entity SpawnPlayer(PlayerSnapshot data, bool isLocal)
    {
        var entity = World.CreatePlayer(base.PlayerIndex, data);
        
        if (isLocal)
        {
            World.Add<LocalPlayerTag>(entity);
            _localPlayer = entity;
            _localNetworkId = data.NetworkId;
        }
        else
            World.Add<RemotePlayerTag>(entity);
        
        return entity;
    }
    
    public bool ApplyPositionFromServer(PlayerPositionSnapshot snapshot)
    {
        if (snapshot.NetworkId == _localNetworkId)
            return false; // Ignora correções para o jogador local (previsão local)
        
        if (!base.PlayerIndex.TryGetEntity(snapshot.NetworkId, out var entity))
            return false;
        
        var player = entity;
        if (!World.IsAlive(player))
            return false;
        
        World.Set(player, snapshot.PositionX, snapshot.PositionY, snapshot.PositionZ);
        return true;
    }
    
}
}