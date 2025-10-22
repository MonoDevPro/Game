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
    private SyncSystem _syncSystem = null!;
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
        _movementSystem = new MovementSystem(world, gameEvents, factory, _mapService);
        systems.Add(_movementSystem);
        
        // Sistemas de saúde
        _healthSystem = new HealthSystem(world, gameEvents, factory);
        systems.Add(_healthSystem);
        
        // Sistemas de combate
        _combatSystem = new CombatSystem(world, gameEvents, factory);
        systems.Add(_combatSystem);
        
        // Sistemas de IA
        _aiSystem = new AISystem(world, gameEvents, factory, _mapService, _combatSystem);
        systems.Add(_aiSystem);

        // Sistemas de sincronização de estado
        _syncSystem = new SyncSystem(world, gameEvents, factory);
        systems.Add(_syncSystem);
    }
    
    public Entity SpawnPlayer(in PlayerCharacter data)
    {
        var entity = EntityFactory.CreatePlayer(data);
        RegisterSpatial(entity);
        return entity;
    }

    public Entity SpawnNpc(NPCCharacter data)
    {
        var entity = EntityFactory.CreateNPC(data);
        RegisterSpatial(entity);
        return entity;
    }

    public Entity SpawnProjectile(ProjectileData data)
    {
        var entity = EntityFactory.CreateProjectile(data);
        if (World.Has<Position>(entity))
            RegisterSpatial(entity);
        return entity;
    }

    public Entity SpawnDroppedItem(DroppedItemData data)
    {
        var entity = EntityFactory.CreateDroppedItem(data);
        if (World.Has<Position>(entity))
            RegisterSpatial(entity);
        return entity;
    }

    public bool DespawnEntity(Entity e)
    {
        if (World.Has<Position>(e))
            UnregisterSpatial(e);
        return EntityFactory.DestroyEntity(e);
    }

    public void ApplyPlayerInput(Entity e, sbyte inputX, sbyte inputY, InputFlags flags)
    {
        if (World.IsAlive(e) && World.Has<PlayerControlled>(e))
        {
            _inputSystem.ApplyPlayerInput(e, inputX, inputY, flags);
        }
    }

    private void RegisterSpatial(Entity entity)
    {
        if (!World.Has<Position>(entity))
            return;

        int mapId = 0;
        if (World.Has<MapId>(entity))
        {
            ref MapId mapComponent = ref World.Get<MapId>(entity);
            mapId = mapComponent.Value;
        }

        if (!_mapService.HasMap(mapId))
        {
            _mapService.RegisterMap(mapId, new MapGrid(100, 100), new MapSpatial());
        }

        var spatial = _mapService.GetMapSpatial(mapId);
        ref Position position = ref World.Get<Position>(entity);
        spatial.Insert(position, entity);
    }

    private void UnregisterSpatial(Entity entity)
    {
        if (!World.Has<Position>(entity))
            return;

        int mapId = 0;
        if (World.Has<MapId>(entity))
        {
            ref MapId mapComponent = ref World.Get<MapId>(entity);
            mapId = mapComponent.Value;
        }

        if (!_mapService.HasMap(mapId))
            return;

        var spatial = _mapService.GetMapSpatial(mapId);
        ref Position position = ref World.Get<Position>(entity);
        spatial.Remove(position, entity);
    }

    public void RegisterMap(int mapId, IMapGrid grid, IMapSpatial spatial)
    {
        _mapService.RegisterMap(mapId, grid, spatial);
    }

    public void UnregisterMap(int mapId)
    {
        _mapService.UnregisterMap(mapId);
    }
}