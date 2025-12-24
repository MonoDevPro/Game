using Arch.Core;
using Arch.System;
using GameECS.Server.Navigation.Components;
using GameECS.Server.Navigation.Systems;
using GameECS.Shared.Navigation.Components;
using GameECS.Shared.Navigation.Core;
using GameECS.Shared.Navigation.Data;
using GameECS.Shared.Navigation.Systems;

namespace GameECS.Server.Navigation;

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

    /// <summary>
    /// Teleporta uma entidade instantaneamente para uma nova posição.
    /// </summary>
    /// <param name="entity">Entidade a ser teleportada.</param>
    /// <param name="target">Posição de destino.</param>
    /// <param name="facingDirection">Direção que a entidade deve ficar olhando após o teleporte.</param>
    /// <returns>Mensagem de teleporte para enviar aos clientes, ou null se falhou.</returns>
    public TeleportMessage? TeleportEntity(Entity entity, GridPosition target, MovementDirection facingDirection = MovementDirection.South)
    {
        if (!_world.IsAlive(entity))
            return null;

        if (!Grid.IsValid(target))
            return null;

        // Verifica se destino está livre
        if (Grid.IsOccupied(target.X, target.Y))
            return null;

        ref var currentPos = ref _world.Get<GridPosition>(entity);
        var oldPos = currentPos;

        // Cancela qualquer movimento/path em andamento
        CancelCurrentMovement(entity);

        // Libera ocupação antiga
        Grid.Release(oldPos, entity.Id);

        // Ocupa nova posição
        if (!Grid.TryOccupy(target, entity.Id))
        {
            // Falha - tenta restaurar posição antiga
            Grid.TryOccupy(oldPos, entity.Id);
            return null;
        }

        // Atualiza posição da entidade
        currentPos = target;

        // Remove tags de movimento se existirem
        if (_world.Has<IsMoving>(entity))
            _world.Remove<IsMoving>(entity);

        if (_world.Has<WaitingForPath>(entity))
            _world.Remove<WaitingForPath>(entity);

        // Adiciona tag de destino alcançado
        if (!_world.Has<ReachedDestination>(entity))
            _world.Add<ReachedDestination>(entity);

        return new TeleportMessage
        {
            EntityId = entity.Id,
            X = (short)target.X,
            Y = (short)target.Y,
            FacingDirection = facingDirection
        };
    }

    /// <summary>
    /// Teleporta forçadamente, mesmo que a célula esteja ocupada (empurra ocupante).
    /// Use com cuidado - pode causar sobreposição temporária.
    /// </summary>
    public TeleportMessage? TeleportEntityForced(Entity entity, GridPosition target, MovementDirection facingDirection = MovementDirection.South)
    {
        if (!_world.IsAlive(entity))
            return null;

        if (!Grid.IsValid(target))
            return null;

        ref var currentPos = ref _world.Get<GridPosition>(entity);
        var oldPos = currentPos;

        // Cancela qualquer movimento/path em andamento
        CancelCurrentMovement(entity);

        // Libera ocupação antiga
        Grid.Release(oldPos, entity.Id);

        // Força ocupação (libera se necessário)
        Grid.ForceOccupy(target, entity.Id);

        // Atualiza posição da entidade
        currentPos = target;

        // Remove tags de movimento
        if (_world.Has<IsMoving>(entity))
            _world.Remove<IsMoving>(entity);

        if (_world.Has<WaitingForPath>(entity))
            _world.Remove<WaitingForPath>(entity);

        if (!_world.Has<ReachedDestination>(entity))
            _world.Add<ReachedDestination>(entity);

        return new TeleportMessage
        {
            EntityId = entity.Id,
            X = (short)target.X,
            Y = (short)target.Y,
            FacingDirection = facingDirection
        };
    }

    private void CancelCurrentMovement(Entity entity)
    {
        // Remove path request pendente
        if (_world.Has<PathRequest>(entity))
            _world.Remove<PathRequest>(entity);

        // Reseta estado do path
        ref var state = ref _world.Get<PathState>(entity);
        state.Status = PathStatus.None;
        state.FailReason = PathFailReason.None;

        // Limpa buffer de path
        ref var buffer = ref _world.Get<GridPathBuffer>(entity);
        buffer.Clear();

        // Reseta movimento em andamento
        ref var movement = ref _world.Get<ServerMovement>(entity);
        movement.Reset();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _systems.Dispose();
    }
}