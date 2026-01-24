using Arch.Core;
using Arch.System;
using Game.Infrastructure.ArchECS.Commons.Components;
using Game.Infrastructure.ArchECS.Services.Map;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Core;
using Game.Infrastructure.ArchECS.Services.Navigation.Systems;
using NavMovementSystem = Game.Infrastructure.ArchECS.Services.Navigation.Systems.NavMovementSystem;
using NavPathRequestSystem = Game.Infrastructure.ArchECS.Services.Navigation.Systems.NavPathRequestSystem;

namespace Game.Infrastructure.ArchECS.Services.Navigation;

/// <summary>
/// Módulo de navegação para SERVIDOR.
/// Integra com MapIndex existente para usar MapGrid/MapSpatial.
/// Tick-based, determinístico, sem visual.
/// </summary>
public sealed class NavigationModule : IDisposable
{
    public PathfindingPool Pool { get; }
    public PathfindingSystem Pathfinder { get; }
    public NavigationConfig Config { get; }

    private readonly World _world;
    private readonly WorldMap _worldMap;
    private int MapId => _worldMap.Id;
    private readonly Group<long> _systems;
    private bool _disposed;

    /// <summary>
    /// Cria módulo de navegação integrado com MapIndex existente.
    /// </summary>
    public NavigationModule(
        World world,
        WorldMap worldMap,
        NavigationConfig? config = null)
    {
        _world = world;
        _worldMap = worldMap;
        Config = config ?? NavigationConfig.Default;

        Pool = new PathfindingPool(
            defaultNodeArraySize: _worldMap.Width * _worldMap.Height,
            defaultPathArraySize: Config.MaxPathLength,
            preWarmCount: Config.ParallelWorkers * 2);

        // Cria adaptador IMapData para o PathfindingSystem
        Pathfinder = new PathfindingSystem(Pool, _worldMap);

        // Sistemas do SERVIDOR (tick-based)
        _systems = new Group<long>(
            "ServerNavigation",
            new NavPathRequestSystem(world, Pathfinder, _worldMap, Config.MaxRequestsPerTick),
            new NavMovementSystem(world, _worldMap)
        );

        _systems.Initialize();
    }

    /// <summary>
    /// Atualiza navegação. Chamado a cada tick do servidor.
    /// </summary>
    public void Tick(long serverTick)
    {
        _systems.BeforeUpdate(in serverTick);
        _systems.Update(in serverTick);
        _systems.AfterUpdate(in serverTick);
    }

    /// <summary>
    /// Adiciona componentes de navegação a uma entidade existente.
    /// Assume que a entidade já tem Position e está registrada no MapSpatial.
    /// </summary>
    public void AddNavigationComponents(Entity entity, Position startPosition, int floor, NavAgentSettings? settings = null)
    {
        if (!_worldMap.AddEntity(startPosition, floor, entity))
            return;
        
        var actualSettings = settings ?? NavAgentSettings.Default;
        _world.Add(entity,
            new MapId { Value = MapId },
            new FloorId { Value = floor },
            startPosition,
            new NavMovementState(),
            new NavPathBuffer(),
            new NavPathState { Status = PathStatus.None },
            actualSettings,
            new NavAgent()
        );
    }

    /// <summary>
    /// Solicita movimento para uma posição. 
    /// </summary>
    public void RequestMove(
        Entity entity,
        int targetX,
        int targetY,
        int targetFloor,
        PathRequestFlags flags = PathRequestFlags.None)
    {
        if (!_worldMap.InBounds(targetX, targetY))
            return;

        ref var state = ref _world.Get<NavPathState>(entity);
        state.Status = PathStatus.Pending;
        state.FailReason = PathFailReason.None;

        ref var buffer = ref _world.Get<NavPathBuffer>(entity);
        buffer.Clear();

        if (_world.Has<NavReachedDestination>(entity))
            _world.Remove<NavReachedDestination>(entity);

        if (_world.Has<NavWaitingToMove>(entity))
            _world.Remove<NavWaitingToMove>(entity);

        _world.Add(entity, new NavPathRequest
        {
            TargetX = targetX,
            TargetY = targetY,
            TargetFloor = targetFloor,
            Flags = flags,
            Priority = PathPriority.Normal
        });
    }

    /// <summary>
    /// Para movimento de uma entidade.
    /// </summary>
    public void StopMovement(Entity entity)
    {
        if (_world.Has<NavPathRequest>(entity))
            _world.Remove<NavPathRequest>(entity);

        ref var state = ref _world.Get<NavPathState>(entity);
        state.Status = PathStatus.Cancelled;

        ref var buffer = ref _world.Get<NavPathBuffer>(entity);
        buffer.Clear();

        ref var movement = ref _world.Get<NavMovementState>(entity);
        movement.Reset();

        if (_world.Has<NavIsMoving>(entity))
            _world.Remove<NavIsMoving>(entity);
    }

    /// <summary>
    /// Remove componentes de navegação de uma entidade.
    /// </summary>
    public void RemoveNavigationComponents(Entity entity)
    {
        if (_world.Has<MapId>(entity))
            _world.Remove<MapId>(entity);
        if (_world.Has<FloorId>(entity))
            _world.Remove<FloorId>(entity);
        if (_world.Has<Position>(entity))
            _world.Remove<Position>(entity);
        if (_world.Has<NavMovementState>(entity))
            _world.Remove<NavMovementState>(entity);
        if (_world.Has<NavPathBuffer>(entity))
            _world.Remove<NavPathBuffer>(entity);
        if (_world.Has<NavPathState>(entity))
            _world.Remove<NavPathState>(entity);
        if (_world.Has<NavAgentSettings>(entity))
            _world.Remove<NavAgentSettings>(entity);
        if (_world.Has<NavAgent>(entity))
            _world.Remove<NavAgent>(entity);
        if (_world.Has<NavPathRequest>(entity))
            _world.Remove<NavPathRequest>(entity);
        if (_world.Has<NavIsMoving>(entity))
            _world.Remove<NavIsMoving>(entity);
        if (_world.Has<NavReachedDestination>(entity))
            _world.Remove<NavReachedDestination>(entity);
        if (_world.Has<NavWaitingToMove>(entity))
            _world.Remove<NavWaitingToMove>(entity);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _systems.Dispose();
    }
}