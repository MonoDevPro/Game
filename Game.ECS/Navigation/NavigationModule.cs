using Arch.Core;
using Arch.System;
using Game.ECS.Components;
using Game.ECS.Navigation.Components;
using Game.ECS.Navigation.Core;
using Game.ECS.Navigation.Core.Contracts;
using Game.ECS.Navigation.Data;
using Game.ECS.Navigation.Systems;
using Game.ECS.Services.Map;

namespace Game.ECS.Navigation;

/// <summary>
/// Módulo de navegação para SERVIDOR.
/// Integra com MapIndex existente para usar MapGrid/MapSpatial.
/// Tick-based, determinístico, sem visual.
/// </summary>
public sealed class NavigationModule : IDisposable
{
    public INavigationGrid Grid { get; }
    public PathfindingPool Pool { get; }
    public PathfindingSystem Pathfinder { get; }
    public NavigationConfig Config { get; }

    private readonly World _world;
    private readonly MapIndex _mapIndex;
    private readonly int _mapId;
    private readonly Group<long> _systems;
    private bool _disposed;

    /// <summary>
    /// Cria módulo de navegação integrado com MapIndex existente.
    /// </summary>
    public NavigationModule(
        World world,
        MapIndex mapIndex,
        int mapId,
        NavigationConfig? config = null)
    {
        _world = world;
        _mapIndex = mapIndex;
        _mapId = mapId;
        Config = config ?? NavigationConfig.Default;

        var mapGrid = mapIndex.GetMapGrid(mapId);
        var mapSpatial = mapIndex.GetMapSpatial(mapId);
        
        // Usa o adaptador que integra com o sistema de mapas existente
        Grid = new NavigationGridAdapter(mapId, mapGrid, mapSpatial);

        Pool = new PathfindingPool(
            defaultNodeArraySize: Grid.Width * Grid.Height,
            defaultPathArraySize: Config.MaxPathLength,
            preWarmCount: Config.ParallelWorkers * 2);

        // Cria adaptador IMapData para o PathfindingSystem
        var mapDataAdapter = new NavigationMapDataAdapter(Grid);
        Pathfinder = new PathfindingSystem(Pool, mapDataAdapter, Grid.Width, Grid.Height);

        // Sistemas do SERVIDOR (tick-based)
        _systems = new Group<long>(
            "ServerNavigation",
            new NavPathRequestSystem(world, Pathfinder, Grid, Config.MaxRequestsPerTick),
            new NavMovementSystem(world, Grid)
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
    public void AddNavigationComponents(Entity entity, NavAgentSettings? settings = null)
    {
        var actualSettings = settings ?? NavAgentSettings.Default;
        
        _world.Add(entity,
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
        int targetZ = 0,
        PathRequestFlags flags = PathRequestFlags.None)
    {
        if (!Grid.IsValidCoord(targetX, targetY))
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
            TargetZ = targetZ,
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

    /// <summary>
    /// Obtém dados para broadcast ao cliente.
    /// </summary>
    public NavMovementSnapshot GetSnapshot(Entity entity, long currentTick)
    {
        var pos = _world.Get<Position>(entity);
        var movement = _world.Get<NavMovementState>(entity);

        return new NavMovementSnapshot
        {
            EntityId = entity.Id,
            CurrentX = (short)pos.X,
            CurrentY = (short)pos.Y,
            CurrentZ = (short)pos.Z,
            TargetX = movement.IsMoving ? (short)movement.TargetCell.X : (short)pos.X,
            TargetY = movement.IsMoving ? (short)movement.TargetCell.Y : (short)pos.Y,
            TargetZ = movement.IsMoving ? (short)movement.TargetCell.Z : (short)pos.Z,
            IsMoving = movement.IsMoving,
            DirectionX = (sbyte)movement.MovementDirection.X,
            DirectionY = (sbyte)movement.MovementDirection.Y,
            TicksRemaining = movement.IsMoving
                ? (ushort)Math.Max(0, movement.EndTick - currentTick)
                : (ushort)0
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _systems.Dispose();
    }
}

/// <summary>
/// Snapshot de movimento para enviar ao cliente.
/// </summary>
public struct NavMovementSnapshot
{
    public int EntityId;
    public short CurrentX;
    public short CurrentY;
    public short CurrentZ;
    public short TargetX;
    public short TargetY;
    public short TargetZ;
    public bool IsMoving;
    public sbyte DirectionX;
    public sbyte DirectionY;
    public ushort TicksRemaining;

    /// <summary>
    /// Calcula duração em segundos baseado nos ticks restantes.
    /// </summary>
    public readonly float GetDurationSeconds(float tickRate)
    {
        return TicksRemaining / tickRate;
    }
}

/// <summary>
/// Adaptador IMapData para usar INavigationGrid.
/// </summary>
internal sealed class NavigationMapDataAdapter(INavigationGrid grid) : IMapData
{
    public bool IsWalkable(int x, int y) => grid.IsWalkable(x, y);
    public float GetMovementCost(int x, int y) => grid.GetMovementCost(x, y);
}