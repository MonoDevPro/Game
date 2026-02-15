using Arch.Core;
using Arch.System;
using Game.Infrastructure.ArchECS.Commons;
using Game.Infrastructure.ArchECS.Services.Entities;
using Game.Infrastructure.ArchECS.Services.Entities.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Core;
using Game.Infrastructure.ArchECS.Services.Navigation.Map;
using Game.Infrastructure.ArchECS.Services.Navigation.Systems;

namespace Game.Infrastructure.ArchECS.Services.Navigation;

public readonly record struct NavEntityState(
    int MapId,
    int FloorId,
    int CurrentX,
    int CurrentY,
    int DirectionX,
    int DirectionY,
    bool IsMoving,
    int TargetX,
    int TargetY,
    float MoveProgress
);

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
    
    private readonly CentralEntityRegistry _registry;

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
            new NavMovementSystem(world, _worldMap),
            new NavDirectionalMovementSystem(world, _worldMap)
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
    /// Adiciona componentes de navegação a uma entidade e registra com ID.
    /// </summary>
    /// <param name="entity">Entity do ECS.</param>
    /// <param name="x">Posição X inicial.</param>
    /// <param name="y">Posição Y inicial.</param>
    /// <param name="dirX">Direção X inicial.</param>
    /// <param name="dirY">Direção Y inicial.</param>
    /// <param name="floor">Andar/floor.</param>
    /// <param name="settings">Configurações do agente (opcional).</param>
    public void AddNavigationComponents(Entity entity, int x, int y, int dirX, int dirY, int floor, NavAgentSettings? settings = null)
    {
        var startPosition = new Position { X = x, Y = y };
        
        if (!_worldMap.AddEntity(startPosition, floor, entity))
            return;
        
        var actualSettings = settings ?? NavAgentSettings.Default;
        _world.Add(entity,
            new MapId { Value = MapId },
            new FloorId { Value = floor },
            startPosition,
            new Direction { X = dirX, Y = dirY },
            new NavMovementState(),
            new NavPathBuffer(),
            new NavPathState { Status = PathStatus.None },
            actualSettings,
            new NavAgent()
        );
    }

    public void GetMovementState(
        Entity entity,
        long serverTick,
        out NavEntityState state)
    {
        bool isMoving = false;
        Position position = _world.Get<Position>(entity);
        Position targetPosition = position;
        Direction direction = default;
        float moveProgress = 0f;

        if (_world.Has<NavMovementState>(entity))
        {
            ref readonly var movement = ref _world.Get<NavMovementState>(entity);
            isMoving = movement.IsMoving;
            targetPosition = movement.TargetCell;
            direction = movement.MovementDirection;
            moveProgress = isMoving && movement.EndTick > movement.StartTick
                ? Math.Clamp(
                    (float)(serverTick - movement.StartTick) / 
                    (movement.EndTick - movement.StartTick), 0f, 1f) 
                : 0f;
        }

        state = new NavEntityState
        {
            MapId = _world.Has<MapId>(entity) ? _world.Get<MapId>(entity).Value : -1,
            FloorId = _world.Has<FloorId>(entity) ? _world.Get<FloorId>(entity).Value : -1,
            CurrentX = position.X,
            CurrentY = position.Y,
            DirectionX = direction.X,
            DirectionY = direction.Y,
            IsMoving = isMoving,
            TargetX = targetPosition.X,
            TargetY = targetPosition.Y,
            MoveProgress = moveProgress,
        };
    }

    #region Movimento Direcional (Manual)

    // === Métodos por Entity ===

    /// <summary>
    /// Move um jogador diretamente para uma posição específica (teleporte).
    /// </summary>
    private void MovePlayerDirectly(Entity entity, int x, int y, int floor)
    {
        // Cancela todo movimento
        StopMovement(entity);
        
        ref var position = ref _world.Get<Position>(entity);
        ref var floorComp = ref _world.Get<FloorId>(entity);
        
        _worldMap.RemoveEntity(position, floorComp.Value, entity);
        
        // Atualiza posição diretamente
        position.X = x;
        position.Y = y;
        floorComp.Value = floor;
        
        _worldMap.AddEntity(position, floorComp.Value, entity);
    }
    
    /// <summary>
    /// Solicita movimento único em uma direção (move uma célula e para).
    /// Usa sistema de movimento direto, sem pathfinding.
    /// </summary>
    public void RequestDirectionalMove(
        Entity entity, 
        Direction direction, 
        PathRequestFlags flags = PathRequestFlags.None)
    {
        RequestDirectionalMove(entity, direction, DirectionalMovementType.Single, flags);
    }
    
    /// <summary>
    /// Solicita movimento direcional com tipo específico.
    /// </summary>
    public void RequestDirectionalMove(
        Entity entity, 
        Direction direction,
        DirectionalMovementType movementType,
        PathRequestFlags flags = PathRequestFlags.None)
    {
        // Valida direção
        if (direction.X == 0 && direction.Y == 0)
            return;
            
        // Cancela pathfinding se estiver ativo
        CancelPathfinding(entity);

        _world.Get<Direction>(entity) = direction;

        // Remove request direcional anterior se existir
        if (_world.Has<NavDirectionalRequest>(entity))
            _world.Remove<NavDirectionalRequest>(entity);
        
        // Adiciona nova request direcional
        _world.Add(entity, new NavDirectionalRequest
        {
            Direction = direction,
            MovementType = movementType,
            Flags = flags
        });
    }
    
    /// <summary>
    /// Inicia movimento contínuo em uma direção.
    /// A entidade continuará movendo nessa direção até StopDirectionalMovement ser chamado.
    /// </summary>
    public void StartContinuousMovement(
        Entity entity,
        Direction direction,
        PathRequestFlags flags = PathRequestFlags.None)
    {
        RequestDirectionalMove(entity, direction, DirectionalMovementType.Continuous, flags);
    }
    
    /// <summary>
    /// Para movimento direcional contínuo.
    /// </summary>
    public void StopDirectionalMovement(Entity entity)
    {
        if (_world.Has<NavDirectionalRequest>(entity))
            _world.Remove<NavDirectionalRequest>(entity);
            
        if (_world.Has<NavDirectionalMode>(entity))
            _world.Remove<NavDirectionalMode>(entity);
    }
    
    /// <summary>
    /// Atualiza direção de movimento contínuo (sem parar o movimento).
    /// </summary>
    public void UpdateMovementDirection(Entity entity, Direction newDirection)
    {
        if (!_world.Has<NavDirectionalRequest>(entity))
            return;
            
        if (newDirection.X == 0 && newDirection.Y == 0)
        {
            StopDirectionalMovement(entity);
            return;
        }
        
        _world.Get<Direction>(entity) = newDirection;
            
        ref var request = ref _world.Get<NavDirectionalRequest>(entity);
        request.Direction = newDirection;
    }
    
    /// <summary>
    /// Verifica se entidade está em modo de movimento direcional.
    /// </summary>
    public bool IsInDirectionalMode(Entity entity) 
        => _world.Has<NavDirectionalMode>(entity);
    
    #endregion
    
    #region Movimento por Pathfinding
    
    // === Métodos por Entity ===

    /// <summary>
    /// Solicita movimento para uma posição via pathfinding.
    /// </summary>
    public void RequestPathfindingMove(
        Entity entity,
        int targetX,
        int targetY,
        int targetFloor,
        PathRequestFlags flags = PathRequestFlags.None)
    {
        if (!_worldMap.InBounds(targetX, targetY, targetFloor))
            return;
            
        // Cancela movimento direcional se estiver ativo
        StopDirectionalMovement(entity);

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
    /// Cancela pathfinding em andamento.
    /// </summary>
    public void CancelPathfinding(Entity entity)
    {
        if (_world.Has<NavPathRequest>(entity))
            _world.Remove<NavPathRequest>(entity);

        ref var state = ref _world.Get<NavPathState>(entity);
        if (state.Status is PathStatus.Pending 
            or PathStatus.InProgress)
        {
            state.Status = PathStatus.Cancelled;
        }

        ref var buffer = ref _world.Get<NavPathBuffer>(entity);
        buffer.Clear();
    }
    
    #endregion
    
    #region Gerenciamento de Entidades
    
    // === Métodos por Entity ===

    /// <summary>
    /// Para todo movimento de uma entidade (direcional e pathfinding).
    /// </summary>
    public void StopMovement(Entity entity)
    {
        // Para movimento direcional
        StopDirectionalMovement(entity);
        
        // Para pathfinding
        CancelPathfinding(entity);

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
        _worldMap.RemoveEntity(
            _world.Get<Position>(entity), 
            _world.Get<FloorId>(entity).Value, 
            entity);

        _world.RemoveIfExists<MapId>(entity);
        _world.RemoveIfExists<FloorId>(entity);
        _world.RemoveIfExists<Position>(entity);
        _world.RemoveIfExists<NavMovementState>(entity);
        _world.RemoveIfExists<NavPathBuffer>(entity);
        _world.RemoveIfExists<NavPathState>(entity);
        _world.RemoveIfExists<NavAgentSettings>(entity);
        _world.RemoveIfExists<NavAgent>(entity);
        _world.RemoveIfExists<NavPathRequest>(entity);
        _world.RemoveIfExists<NavIsMoving>(entity);
        _world.RemoveIfExists<NavReachedDestination>(entity);
        _world.RemoveIfExists<NavWaitingToMove>(entity);
        _world.RemoveIfExists<NavDirectionalRequest>(entity);
        _world.RemoveIfExists<NavDirectionalMode>(entity);
    }
    
    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _registry.Clear();
        _systems.Dispose();
    }
}
