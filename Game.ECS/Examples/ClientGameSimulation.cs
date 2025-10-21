using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Data;
using Game.ECS.Systems;
using Arch.Core;
using Arch.System;
using Game.ECS.Entities;
using Game.ECS.Entities.Factories;

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
    
    public ClientGameSimulation()
    {
        ConfigureSystems(World, Events, EntityFactory, Systems);
    }

    /// <summary>
    /// Configura sistemas apenas para o cliente.
    /// Ordem: Input → Movement (previsão local) → Sync (recebe correções do servidor)
    /// </summary>
    protected override void ConfigureSystems(World world, GameEventSystem gameEvents, EntityFactory factory, Group<float> systems)
    {
        // Sistemas de entrada do jogador
        _inputSystem = new InputSystem(world, gameEvents, factory);
        systems.Add(_inputSystem);
        
        // Sistemas de movimento (previsão local)
        _movementSystem = new MovementSystem(world, gameEvents, factory);
        systems.Add(_movementSystem);
    }
    
    public Entity SpawnPlayer(in PlayerCharacter data, bool isLocal)
    {
        var entity = EntityFactory.CreatePlayer(data);
        
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
    
    public Entity SpawnNpc(NPCCharacter data)
    {
        var entity = EntityFactory.CreateNPC(data);
        return entity;
    }

    public Entity SpawnProjectile(ProjectileData data)
    {
        var entity = EntityFactory.CreateProjectile(data);
        return entity;
    }

    public Entity SpawnDroppedItem(DroppedItemData data)
    {
        var entity = EntityFactory.CreateDroppedItem(data);
        return entity;
    }
    
    public bool DespawnEntity(Entity e)
    {
        return EntityFactory.DestroyEntity(e);
    }

    public bool ApplyPlayerInput(sbyte inputX, sbyte inputY, InputFlags flags)
    {
        return _inputSystem.ApplyPlayerInput(_localPlayer, inputX, inputY, flags);
    }
    
    public bool ClearPlayerInput()
    {
        return _inputSystem.ClearPlayerInput(_localPlayer);
    }
}