using Arch.Core;
using Arch.System;
using Game.Infrastructure.ArchECS.Services.EntityRegistry;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Core;
using Game.Infrastructure.ArchECS.Services.Navigation.Map;
using Game.Infrastructure.ArchECS.Services.Navigation.Systems;

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
        
        _registry = world.GetEntityRegistry();

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
    /// <param name="startPosition">Posição inicial.</param>
    /// <param name="startDirection">Direção inicial.</param>
    /// <param name="floor">Andar/floor.</param>
    /// <param name="settings">Configurações do agente (opcional).</param>
    public void AddNavigationComponents(Entity entity, Position startPosition, Direction startDirection, int floor, NavAgentSettings? settings = null)
    {
        if (!_worldMap.AddEntity(startPosition, floor, entity))
            return;
        
        var actualSettings = settings ?? NavAgentSettings.Default;
        _world.Add(entity,
            new MapId { Value = MapId },
            new FloorId { Value = floor },
            startPosition,
            startDirection,
            new NavMovementState(),
            new NavPathBuffer(),
            new NavPathState { Status = PathStatus.None },
            actualSettings,
            new NavAgent()
        );
    }
    
    #region Movimento Direcional (Manual)
    
    // === Sobrecargas por ID ===
    
    /// <summary>Solicita movimento único em uma direção.</summary>
    public void RequestDirectionalMove(int entityId, Direction direction, PathRequestFlags flags = PathRequestFlags.None)
        => RequestDirectionalMove(_registry.GetEntity(entityId, EntityDomain.Navigation), direction, flags);
    
    /// <summary>Solicita movimento direcional com tipo específico.</summary>
    public void RequestDirectionalMove(int entityId, Direction direction, DirectionalMovementType movementType, PathRequestFlags flags = PathRequestFlags.None)
        => RequestDirectionalMove(_registry.GetEntity(entityId, EntityDomain.Navigation), direction, movementType, flags);
    
    /// <summary>Inicia movimento contínuo em uma direção.</summary>
    public void StartContinuousMovement(int entityId, Direction direction, PathRequestFlags flags = PathRequestFlags.None)
        => StartContinuousMovement(_registry.GetEntity(entityId, EntityDomain.Navigation), direction, flags);
    
    /// <summary>Para movimento direcional contínuo.</summary>
    public void StopDirectionalMovement(int entityId)
        => StopDirectionalMovement(_registry.GetEntity(entityId, EntityDomain.Navigation));
    
    /// <summary>Atualiza direção de movimento contínuo.</summary>
    public void UpdateMovementDirection(int entityId, Direction newDirection)
        => UpdateMovementDirection(_registry.GetEntity(entityId, EntityDomain.Navigation), newDirection);
    
    /// <summary>Verifica se entidade está em modo de movimento direcional.</summary>
    public bool IsInDirectionalMode(int entityId)
        => IsInDirectionalMode(_registry.GetEntity(entityId, EntityDomain.Navigation));
    
    public void MovePlayerDirectly(int entityId, int x, int y, int floor)
        => MovePlayerDirectly(_registry.GetEntity(entityId, EntityDomain.Navigation), x, y, floor);
    
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

        // Garante modo direcional ativo para o sistema processar o request
        if (!_world.Has<NavDirectionalMode>(entity))
            _world.Add<NavDirectionalMode>(entity);
        
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
    
    // === Sobrecargas por ID ===
    
    /// <summary>Solicita movimento para uma posição via pathfinding.</summary>
    public void RequestPathfindingMove(int entityId, int targetX, int targetY, int targetFloor, PathRequestFlags flags = PathRequestFlags.None)
        => RequestPathfindingMove(_registry.GetEntity(entityId, EntityDomain.Navigation), targetX, targetY, targetFloor, flags);
    
    /// <summary>Solicita movimento para uma posição via pathfinding.</summary>
    public void RequestPathfindingMove(int entityId, Position target, int targetFloor, PathRequestFlags flags = PathRequestFlags.None)
        => RequestPathfindingMove(_registry.GetEntity(entityId, EntityDomain.Navigation), target.X, target.Y, targetFloor, flags);
    
    /// <summary>Cancela pathfinding em andamento.</summary>
    public void CancelPathfinding(int entityId)
        => CancelPathfinding(_registry.GetEntity(entityId, EntityDomain.Navigation));
    
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
    
    // === Sobrecargas por ID ===
    
    /// <summary>Para todo movimento de uma entidade.</summary>
    public void StopMovement(int entityId)
        => StopMovement(_registry.GetEntity(entityId, EntityDomain.Navigation));
    
    public bool TryRemoveNavigationComponents(int entityId, out Entity entity)
    {
        if (!_registry.TryGetEntity(entityId, EntityDomain.Navigation, out entity))
            return false;
        
        _worldMap.RemoveEntity(
            _world.Get<Position>(entity),
            _world.Get<FloorId>(entity).Value,
            entity);
        
        RemoveNavigationComponents(entity);
        _registry.Unregister(entity);
        return true;
    }
    
    // === Métodos por Entity ===

    /// <summary>
    /// Para todo movimento de uma entidade (direcional e pathfinding).
    /// </summary>
    private void StopMovement(Entity entity)
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
        if (_world.Has<NavDirectionalRequest>(entity))
            _world.Remove<NavDirectionalRequest>(entity);
        if (_world.Has<NavDirectionalMode>(entity))
            _world.Remove<NavDirectionalMode>(entity);
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
