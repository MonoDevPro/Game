using Arch.Core;
using Arch.System;
using Game.ECS. Navigation.Components;
using Game.ECS.Navigation.Core;
using Game. ECS.Navigation. Data;
using Game.ECS.Navigation.Systems;

namespace Game.ECS.Navigation;

/// <summary>
/// Módulo de navegação para SERVIDOR.
/// Tick-based, determinístico, sem visual.
/// </summary>
public sealed class ServerNavigationModule : IDisposable
{
    public NavigationGrid Grid { get; }
    public PathfindingPool Pool { get; }
    public GridPathfindingSystem Pathfinder { get; }
    public NavigationConfig Config { get; }

    private readonly World _world;
    private readonly Group<long> _systems;
    private bool _disposed;

    public ServerNavigationModule(
        World world,
        int gridWidth,
        int gridHeight,
        NavigationConfig?  config = null)
    {
        _world = world;
        Config = config ?? NavigationConfig.Default;

        Grid = new NavigationGrid(gridWidth, gridHeight, cellSize: 1f);
        Pool = new PathfindingPool(
            nodeCapacity: gridWidth * gridHeight,
            pathCapacity: Config.MaxPathLength,
            preWarmCount: Config.ParallelWorkers * 2);

        Pathfinder = new GridPathfindingSystem(Grid, Pool, Config);

        // Sistemas do SERVIDOR (tick-based)
        _systems = new Group<long>(
            "ServerNavigation",
            new ServerPathRequestSystem(world, Pathfinder, Config.MaxRequestsPerTick),
            new ServerMovementSystem(world, Grid)
        );

        _systems.Initialize();
    }

    /// <summary>
    /// Atualiza navegação.  Chamado a cada tick do servidor.
    /// </summary>
    public void Tick(long serverTick)
    {
        _systems.BeforeUpdate(in serverTick);
        _systems.Update(in serverTick);
        _systems.AfterUpdate(in serverTick);
    }

    /// <summary>
    /// Cria agente de navegação. 
    /// </summary>
    public Entity CreateAgent(int gridX, int gridY, ServerAgentSettings?  settings = null)
    {
        var actualSettings = settings ?? ServerAgentSettings.Default;

        var entity = _world.Create(
            new GridPosition(gridX, gridY),
            new ServerMovementState(),
            new GridPathBuffer(),
            new PathState { Status = PathStatus. None },
            actualSettings,
            new GridNavigationAgent()
        );

        // Ocupa célula inicial
        Grid.TryOccupy(gridX, gridY, entity. Id);

        return entity;
    }

    /// <summary>
    /// Solicita movimento para uma posição. 
    /// </summary>
    public void RequestMove(
        Entity entity,
        int targetX,
        int targetY,
        PathRequestFlags flags = PathRequestFlags.None)
    {
        if (!Grid.IsValidCoord(targetX, targetY))
            return;

        ref var state = ref _world. Get<PathState>(entity);
        state.Status = PathStatus.Pending;
        state.FailReason = PathFailReason.None;

        ref var buffer = ref _world.Get<GridPathBuffer>(entity);
        buffer.Clear();

        if (_world.Has<ReachedDestination>(entity))
            _world.Remove<ReachedDestination>(entity);

        if (_world.Has<WaitingToMove>(entity))
            _world.Remove<WaitingToMove>(entity);

        _world.Add(entity, new PathRequest
        {
            TargetX = targetX,
            TargetY = targetY,
            Flags = flags,
            Priority = PathPriority.Normal
        });
    }

    /// <summary>
    /// Para movimento de uma entidade.
    /// </summary>
    public void StopMovement(Entity entity)
    {
        if (_world.Has<PathRequest>(entity))
            _world.Remove<PathRequest>(entity);

        ref var state = ref _world.Get<PathState>(entity);
        state.Status = PathStatus.Cancelled;

        ref var buffer = ref _world.Get<GridPathBuffer>(entity);
        buffer.Clear();

        ref var movement = ref _world.Get<ServerMovementState>(entity);
        movement.Reset();

        if (_world. Has<IsMoving>(entity))
            _world.Remove<IsMoving>(entity);
    }

    /// <summary>
    /// Remove agente e libera célula.
    /// </summary>
    public void RemoveAgent(Entity entity)
    {
        var pos = _world.Get<GridPosition>(entity);
        Grid.Release(pos.X, pos.Y, entity.Id);
        _world. Destroy(entity);
    }

    /// <summary>
    /// Obtém dados para broadcast ao cliente.
    /// </summary>
    public MovementSnapshot GetSnapshot(Entity entity, long currentTick)
    {
        var pos = _world.Get<GridPosition>(entity);
        var movement = _world.Get<ServerMovementState>(entity);

        return new MovementSnapshot
        {
            EntityId = entity.Id,
            CurrentX = (short)pos.X,
            CurrentY = (short)pos.Y,
            TargetX = movement.IsMoving ? (short)movement.TargetCell.X : (short)pos.X,
            TargetY = movement.IsMoving ? (short)movement.TargetCell.Y : (short)pos.Y,
            IsMoving = movement.IsMoving,
            Direction = movement.Direction,
            TicksRemaining = movement.IsMoving
                ? (ushort)Math.Max(0, movement. EndTick - currentTick)
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
public struct MovementSnapshot
{
    public int EntityId;
    public short CurrentX;
    public short CurrentY;
    public short TargetX;
    public short TargetY;
    public bool IsMoving;
    public MovementDirection Direction;
    public ushort TicksRemaining;

    /// <summary>
    /// Calcula duração em segundos baseado nos ticks restantes.
    /// </summary>
    public readonly float GetDurationSeconds(float tickRate)
    {
        return TicksRemaining / tickRate;
    }

}