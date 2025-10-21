using Game.ECS.Components;
using Game.ECS.Entities.Data;
using Game.ECS.Services;
using Game.ECS.Systems;
using Arch.Core;
using Arch.System;
using Game.ECS.Entities.Factories;

namespace Game.ECS.Examples;

/// <summary>
/// Exemplo de uso do ECS como SERVIDOR.
/// O servidor executa a simulação completa com todos os sistemas.
/// </summary>
public sealed class ServerGameSimulation : GameSimulation
{
    private InputSystem _inputSystem = null!;
    private MovementSystem _movementSystem = null!;
    private HealthSystem _healthSystem = null!;
    private CombatSystem _combatSystem = null!;
    private AISystem _aiSystem = null!;
    private readonly IMapService _mapService;
    
    public ServerGameSimulation()
    {
        // Registra todos os serviços
        _mapService = new MapService();
        // Configura os sistemas
        ConfigureSystems(World, Events, EntityFactory, Systems);
    }

    /// <summary>
    /// Configura todos os sistemas de servidor.
    /// Ordem importante: Input → Movement → Combat → Sync
    /// </summary>
    protected override void ConfigureSystems(World world, GameEventSystem gameEvents, EntityFactory factory, Group<float> systems)
    {
        // Sistemas de entrada (input não vem do servidor, vem do cliente)
        // Mas o servidor valida e aplica
        _inputSystem = new InputSystem(world, gameEvents, factory);
        systems.Add(_inputSystem);
        
        // Sistemas de movimento
        _movementSystem = new MovementSystem(world, gameEvents, factory);
        systems.Add(_movementSystem);
        
        // Sistemas de saúde
        _healthSystem = new HealthSystem(world, gameEvents, factory);
        systems.Add(_healthSystem);
        
        // Sistemas de combate
        _combatSystem = new CombatSystem(world, gameEvents, factory);
        systems.Add(_combatSystem);
        
        // Sistemas de IA
        _aiSystem = new AISystem(world, gameEvents, factory);
        systems.Add(_aiSystem);
    }
    
    public Entity SpawnPlayer(in PlayerCharacter data)
    {
        var entity = EntityFactory.CreatePlayer(data);
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

    public void ApplyPlayerInput(Entity e, sbyte inputX, sbyte inputY, InputFlags flags)
    {
        if (World.IsAlive(e) && World.Has<PlayerControlled>(e))
        {
            _inputSystem.ApplyPlayerInput(e, inputX, inputY, flags);
        }
    }
}