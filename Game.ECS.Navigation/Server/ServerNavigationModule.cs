using Arch.Core;
using Arch.System;
using Game.ECS.Navigation.Server.Components;
using Game.ECS.Navigation.Server.Systems;
using Game.ECS.Navigation.Shared.Components;
using Game.ECS.Navigation.Shared.Core;
using Game.ECS.Navigation.Shared.Data;
using Game.ECS.Navigation.Shared.Systems;

namespace Game.ECS.Navigation.Server;

/// <summary>
/// Módulo de navegação do servidor.
/// </summary>
public sealed class ServerNavigationModule : IDisposable
{
    public NavigationGrid Grid { get; }
    public PathfindingService Pathfinder { get; }
    public NavigationConfig Config { get; }

    private readonly World _world;
    private readonly Group<long> _systems;
    private bool _disposed;

    public ServerNavigationModule(World world, int width, int height, NavigationConfig? config = null)
    {
        _world = world;
        Config = config ?? NavigationConfig.Default;

        Grid = new NavigationGrid(width, height);
        var pool = new PathfindingPool(width * height, Config.MaxPathLength, Config.PoolPrewarmCount);
        Pathfinder = new PathfindingService(Grid, pool, Config);

        _systems = new Group<long>("ServerNavigation",
            new ServerPathRequestSystem(world, Pathfinder, Config.MaxRequestsPerTick),
            new ServerMovementSystem(world, Grid));

        _systems.Initialize();
    }

    public void Tick(long serverTick)
    {
        _systems.BeforeUpdate(in serverTick);
        _systems.Update(in serverTick);
        _systems.AfterUpdate(in serverTick);
    }

    public Entity CreateAgent(GridPosition position, ServerAgentConfig? config = null)
    {
        var entity = _world.Create(
            position,
            new ServerMovement(),
            new GridPathBuffer(),
            new PathState(),
            config ?? ServerAgentConfig.Default,
            new NavigationAgent());

        Grid.TryOccupy(position, entity.Id);
        return entity;
    }

    public void RequestMoveTo(Entity entity, GridPosition target, PathRequestFlags flags = PathRequestFlags.None)
    {
        if (!Grid.IsValid(target)) return;

        ref var state = ref _world.Get<PathState>(entity);
        state.Status = PathStatus.Pending;
        state.FailReason = PathFailReason.None;

        ref var buffer = ref _world.Get<GridPathBuffer>(entity);
        buffer.Clear();

        if (_world.Has<ReachedDestination>(entity))
            _world.Remove<ReachedDestination>(entity);

        _world.Add(entity, PathRequest.Create(target, PathPriority.Normal, flags));
    }

    public void StopAgent(Entity entity)
    {
        if (_world.Has<PathRequest>(entity))
            _world.Remove<PathRequest>(entity);

        ref var state = ref _world.Get<PathState>(entity);
        state.Status = PathStatus.Cancelled;

        ref var buffer = ref _world.Get<GridPathBuffer>(entity);
        buffer.Clear();

        ref var movement = ref _world.Get<ServerMovement>(entity);
        movement.Reset();

        if (_world.Has<IsMoving>(entity))
            _world.Remove<IsMoving>(entity);
    }

    public void RemoveAgent(Entity entity)
    {
        var pos = _world.Get<GridPosition>(entity);
        Grid.Release(pos, entity.Id);
        _world.Destroy(entity);
    }

    public MovementSnapshot GetSnapshot(Entity entity, long currentTick)
    {
        var pos = _world.Get<GridPosition>(entity);
        var movement = _world.Get<ServerMovement>(entity);

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