using Arch.Core;
using Game.ECS.Navigation.Server.Components;
using Game.ECS.Navigation.Server.Systems;
using Game.ECS.Navigation.Shared.Components;
using Game.ECS.Navigation.Shared.Core;
using Game.ECS.Navigation.Shared.Systems;

namespace Game.ECS.Navigation.Tests.Server.Systems;

#region ServerMovementSystem Tests

public class ServerMovementSystemTests : IDisposable
{
    private readonly World _world;
    private readonly NavigationGrid _grid;
    private readonly ServerMovementSystem _system;

    public ServerMovementSystemTests()
    {
        _world = World.Create();
        _grid = new NavigationGrid(100, 100);
        _system = new ServerMovementSystem(_world, _grid);
    }

    public void Dispose()
    {
        _world.Dispose();
    }

    private Entity CreateMovingAgent(int startX, int startY)
    {
        return _world.Create(
            new NavigationAgent(),
            new GridPosition(startX, startY),
            new ServerMovement(),
            new GridPathBuffer(),
            new PathState(),
            new ServerAgentConfig 
            { 
                CardinalMoveTicks = 6, 
                DiagonalMoveTicks = 9,
                AllowDiagonal = true,
                MaxRetries = 3
            }
        );
    }

    [Fact]
    public void Update_NoMovingEntities_ShouldNotThrow()
    {
        // Arrange
        CreateMovingAgent(5, 5);

        // Act & Assert
        var exception = Record.Exception(() => _system.Update(1));
        Assert.Null(exception);
    }

    [Fact]
    public void Update_MovementInProgress_ShouldNotComplete()
    {
        // Arrange
        var entity = CreateMovingAgent(5, 5);
        ref var movement = ref _world.Get<ServerMovement>(entity);
        movement.Start(
            new GridPosition(5, 5),
            new GridPosition(6, 5),
            currentTick: 1,
            durationTicks: 6
        );

        // Act - Tick 3, ainda não deve completar (precisa tick 7)
        _system.Update(3);

        // Assert
        var pos = _world.Get<GridPosition>(entity);
        Assert.Equal(5, pos.X);
        Assert.Equal(5, pos.Y);
        Assert.True(_world.Get<ServerMovement>(entity).IsMoving);
    }

    [Fact]
    public void Update_MovementComplete_ShouldUpdatePosition()
    {
        // Arrange
        var entity = CreateMovingAgent(5, 5);
        ref var movement = ref _world.Get<ServerMovement>(entity);
        movement.Start(
            new GridPosition(5, 5),
            new GridPosition(6, 5),
            currentTick: 1,
            durationTicks: 6
        );

        // Act - Tick 7, deve completar (1 + 6 = 7)
        _system.Update(7);

        // Assert
        var pos = _world.Get<GridPosition>(entity);
        Assert.Equal(6, pos.X);
        Assert.Equal(5, pos.Y);
        Assert.False(_world.Get<ServerMovement>(entity).IsMoving);
    }

    [Fact]
    public void Update_PathFollowing_ShouldStartNextMovement()
    {
        // Arrange
        var entity = CreateMovingAgent(5, 5);
        _grid.TryOccupy(new GridPosition(5, 5), entity.Id);
        
        ref var pathBuffer = ref _world.Get<GridPathBuffer>(entity);
        pathBuffer.Clear();
        // Adiciona caminho: (5,5) -> (6,5) -> (7,5)
        pathBuffer.SetWaypoint(0, 5 + 5 * 100); // Start (será pulado pois é posição atual)
        pathBuffer.SetWaypoint(1, 6 + 5 * 100); // Next
        pathBuffer.SetWaypoint(2, 7 + 5 * 100); // End
        pathBuffer.WaypointCount = 3;
        pathBuffer.CurrentIndex = 1; // Começa no próximo waypoint

        ref var state = ref _world.Get<PathState>(entity);
        state.Status = PathStatus.Ready;

        // Act
        _system.Update(1);

        // Assert
        var movement = _world.Get<ServerMovement>(entity);
        Assert.True(movement.IsMoving);
        Assert.Equal(6, movement.TargetCell.X);
    }

    [Fact]
    public void Update_PathComplete_ShouldSetCompletedStatus()
    {
        // Arrange
        var entity = CreateMovingAgent(5, 5);
        
        ref var pathBuffer = ref _world.Get<GridPathBuffer>(entity);
        pathBuffer.Clear();
        // Path já percorrido
        pathBuffer.SetWaypoint(0, 5 + 5 * 100);
        pathBuffer.WaypointCount = 1;
        pathBuffer.TryAdvance(); // Avança para além do path

        ref var state = ref _world.Get<PathState>(entity);
        state.Status = PathStatus.Ready;

        // Act
        _system.Update(1);

        // Assert
        state = ref _world.Get<PathState>(entity);
        Assert.Equal(PathStatus.Completed, state.Status);
    }

    [Fact]
    public void Update_PathBlocked_ShouldAddWaitingComponent()
    {
        // Arrange
        var entity1 = CreateMovingAgent(5, 5);
        var entity2 = CreateMovingAgent(6, 5);
        
        _grid.TryOccupy(new GridPosition(5, 5), entity1.Id);
        _grid.TryOccupy(new GridPosition(6, 5), entity2.Id); // Bloqueador
        
        ref var pathBuffer = ref _world.Get<GridPathBuffer>(entity1);
        pathBuffer.Clear();
        pathBuffer.SetWaypoint(0, 5 + 5 * 100);
        pathBuffer.SetWaypoint(1, 6 + 5 * 100); // Célula bloqueada
        pathBuffer.WaypointCount = 2;
        pathBuffer.CurrentIndex = 1; // Próximo waypoint é o bloqueado

        ref var state = ref _world.Get<PathState>(entity1);
        state.Status = PathStatus.Ready;

        // Act
        _system.Update(1);

        // Assert
        Assert.True(_world.Has<WaitingForPath>(entity1));
        var waiting = _world.Get<WaitingForPath>(entity1);
        Assert.Equal(entity2.Id, waiting.BlockerId);
    }

    [Fact]
    public void Update_PathUnblocked_ShouldRemoveWaitingComponent()
    {
        // Arrange
        var entity = CreateMovingAgent(5, 5);
        _grid.TryOccupy(new GridPosition(5, 5), entity.Id);
        
        // Adiciona componente de espera
        _world.Add(entity, new WaitingForPath { StartTick = 1, BlockerId = 999 });
        
        ref var pathBuffer = ref _world.Get<GridPathBuffer>(entity);
        pathBuffer.Clear();
        pathBuffer.SetWaypoint(0, 5 + 5 * 100);
        pathBuffer.SetWaypoint(1, 6 + 5 * 100); // Agora livre
        pathBuffer.WaypointCount = 2;
        pathBuffer.CurrentIndex = 1; // Próximo waypoint

        ref var state = ref _world.Get<PathState>(entity);
        state.Status = PathStatus.Ready;

        // Act
        _system.Update(1);

        // Assert
        Assert.False(_world.Has<WaitingForPath>(entity));
    }

    [Fact]
    public void Update_StartMovement_ShouldAddIsMovingTag()
    {
        // Arrange
        var entity = CreateMovingAgent(5, 5);
        _grid.TryOccupy(new GridPosition(5, 5), entity.Id);
        
        ref var pathBuffer = ref _world.Get<GridPathBuffer>(entity);
        pathBuffer.Clear();
        pathBuffer.SetWaypoint(0, 5 + 5 * 100);
        pathBuffer.SetWaypoint(1, 6 + 5 * 100);
        pathBuffer.WaypointCount = 2;
        pathBuffer.CurrentIndex = 1; // Próximo waypoint

        ref var state = ref _world.Get<PathState>(entity);
        state.Status = PathStatus.Ready;

        // Act
        _system.Update(1);

        // Assert
        Assert.True(_world.Has<IsMoving>(entity));
    }

    [Fact]
    public void Update_FinishNavigation_ShouldAddReachedDestinationTag()
    {
        // Arrange
        var entity = CreateMovingAgent(5, 5);
        
        ref var pathBuffer = ref _world.Get<GridPathBuffer>(entity);
        pathBuffer.Clear();
        // Path vazio/completo

        ref var state = ref _world.Get<PathState>(entity);
        state.Status = PathStatus.Ready;

        // Act
        _system.Update(1);

        // Assert
        Assert.True(_world.Has<ReachedDestination>(entity));
    }

    [Fact]
    public void Update_DiagonalMovement_ShouldUseDiagonalTicks()
    {
        // Arrange
        var entity = CreateMovingAgent(5, 5);
        _grid.TryOccupy(new GridPosition(5, 5), entity.Id);
        
        ref var pathBuffer = ref _world.Get<GridPathBuffer>(entity);
        pathBuffer.Clear();
        pathBuffer.SetWaypoint(0, 5 + 5 * 100); // (5,5)
        pathBuffer.SetWaypoint(1, 6 + 6 * 100); // (6,6) - diagonal
        pathBuffer.WaypointCount = 2;
        pathBuffer.CurrentIndex = 1; // Próximo waypoint é diagonal

        ref var state = ref _world.Get<PathState>(entity);
        state.Status = PathStatus.Ready;

        // Act
        _system.Update(1);

        // Assert
        var movement = _world.Get<ServerMovement>(entity);
        Assert.True(movement.IsMoving);
        // Movimento diagonal deve ter mais ticks (9 vs 6)
        Assert.True(movement.ShouldComplete(10)); // tick 1 + 9 = 10
        Assert.False(movement.ShouldComplete(9)); // tick 1 + 9 = 10, então 9 não completa
    }

    [Fact]
    public void Update_MultipleEntities_ShouldProcessAll()
    {
        // Arrange
        var entity1 = CreateMovingAgent(5, 5);
        var entity2 = CreateMovingAgent(10, 10);
        
        ref var movement1 = ref _world.Get<ServerMovement>(entity1);
        movement1.Start(new GridPosition(5, 5), new GridPosition(6, 5), 1, 6);
        
        ref var movement2 = ref _world.Get<ServerMovement>(entity2);
        movement2.Start(new GridPosition(10, 10), new GridPosition(11, 10), 1, 6);

        // Act
        _system.Update(7);

        // Assert
        Assert.Equal(6, _world.Get<GridPosition>(entity1).X);
        Assert.Equal(11, _world.Get<GridPosition>(entity2).X);
    }

    [Fact]
    public void Update_FollowingStatus_ShouldContinueProcessing()
    {
        // Arrange
        var entity = CreateMovingAgent(5, 5);
        _grid.TryOccupy(new GridPosition(5, 5), entity.Id);
        
        ref var pathBuffer = ref _world.Get<GridPathBuffer>(entity);
        pathBuffer.Clear();
        pathBuffer.SetWaypoint(0, 5 + 5 * 100);
        pathBuffer.SetWaypoint(1, 6 + 5 * 100);
        pathBuffer.WaypointCount = 2;
        pathBuffer.CurrentIndex = 1; // Próximo waypoint

        ref var state = ref _world.Get<PathState>(entity);
        state.Status = PathStatus.Following; // Já está seguindo

        // Act
        _system.Update(1);

        // Assert
        var movement = _world.Get<ServerMovement>(entity);
        Assert.True(movement.IsMoving);
    }

    [Fact]
    public void Update_PendingStatus_ShouldNotProcess()
    {
        // Arrange
        var entity = CreateMovingAgent(5, 5);
        
        ref var pathBuffer = ref _world.Get<GridPathBuffer>(entity);
        pathBuffer.Clear();
        pathBuffer.SetWaypoint(0, 5 + 5 * 100);
        pathBuffer.SetWaypoint(1, 6 + 5 * 100);
        pathBuffer.WaypointCount = 2;

        ref var state = ref _world.Get<PathState>(entity);
        state.Status = PathStatus.Pending; // Aguardando processamento

        // Act
        _system.Update(1);

        // Assert
        var movement = _world.Get<ServerMovement>(entity);
        Assert.False(movement.IsMoving);
    }
}

#endregion

#region ServerPathRequestSystem Tests

public class ServerPathRequestSystemTests : IDisposable
{
    private readonly World _world;
    private readonly NavigationGrid _grid;
    private readonly PathfindingPool _pool;
    private readonly PathfindingService _pathfinder;
    private readonly ServerPathRequestSystem _system;

    public ServerPathRequestSystemTests()
    {
        _world = World.Create();
        _grid = new NavigationGrid(100, 100);
        _pool = new PathfindingPool(_grid.TotalCells);
        _pathfinder = new PathfindingService(_grid, _pool);
        _system = new ServerPathRequestSystem(_world, _pathfinder, maxPerTick: 50);
    }

    public void Dispose()
    {
        _world.Dispose();
    }

    private Entity CreateAgentWithRequest(int startX, int startY, int targetX, int targetY)
    {
        var entity = _world.Create(
            new NavigationAgent(),
            new GridPosition(startX, startY),
            new PathRequest { TargetX = targetX, TargetY = targetY },
            new PathState { Status = PathStatus.Pending },
            new GridPathBuffer()
        );
        return entity;
    }

    [Fact]
    public void Update_ValidPathRequest_ShouldFindPath()
    {
        // Arrange
        var entity = CreateAgentWithRequest(5, 5, 10, 5);

        // Act
        _system.BeforeUpdate(1);
        _system.Update(1);

        // Assert
        var state = _world.Get<PathState>(entity);
        Assert.Equal(PathStatus.Ready, state.Status);
        Assert.False(_world.Has<PathRequest>(entity));
    }

    [Fact]
    public void Update_InvalidPathRequest_ShouldSetFailed()
    {
        // Arrange
        // Bloqueia destino
        _grid.SetWalkable(10, 5, false);
        var entity = CreateAgentWithRequest(5, 5, 10, 5);

        // Act
        _system.BeforeUpdate(1);
        _system.Update(1);

        // Assert
        var state = _world.Get<PathState>(entity);
        Assert.Equal(PathStatus.Failed, state.Status);
        Assert.NotEqual(PathFailReason.None, state.FailReason);
    }

    [Fact]
    public void Update_ShouldRemovePathRequestComponent()
    {
        // Arrange
        var entity = CreateAgentWithRequest(5, 5, 10, 5);

        // Act
        _system.BeforeUpdate(1);
        _system.Update(1);

        // Assert
        Assert.False(_world.Has<PathRequest>(entity));
    }

    [Fact]
    public void Update_ShouldFillPathBuffer()
    {
        // Arrange
        var entity = CreateAgentWithRequest(5, 5, 10, 5);

        // Act
        _system.BeforeUpdate(1);
        _system.Update(1);

        // Assert
        var buffer = _world.Get<GridPathBuffer>(entity);
        Assert.True(buffer.IsValid);
        Assert.True(buffer.WaypointCount > 0);
    }

    [Fact]
    public void Update_ShouldRespectMaxPerTick()
    {
        // Arrange - Cria mais requisições que o limite
        var entities = new Entity[60];
        for (int i = 0; i < 60; i++)
        {
            entities[i] = CreateAgentWithRequest(i % 10, i / 10, 50, 50);
        }

        // Act
        _system.BeforeUpdate(1);
        _system.Update(1);

        // Assert - Apenas 50 devem ter sido processados
        int processed = 0;
        int pending = 0;
        foreach (var entity in entities)
        {
            var state = _world.Get<PathState>(entity);
            if (state.Status == PathStatus.Ready || state.Status == PathStatus.Failed)
                processed++;
            else if (state.Status == PathStatus.Pending)
                pending++;
        }

        Assert.Equal(50, processed);
        Assert.Equal(10, pending);
    }

    [Fact]
    public void Update_NonPendingStatus_ShouldNotProcess()
    {
        // Arrange
        var entity = CreateAgentWithRequest(5, 5, 10, 5);
        ref var state = ref _world.Get<PathState>(entity);
        state.Status = PathStatus.Computing; // Já está computando

        // Act
        _system.BeforeUpdate(1);
        _system.Update(1);

        // Assert - Ainda deve ter o PathRequest
        Assert.True(_world.Has<PathRequest>(entity));
    }

    [Fact]
    public void Update_ShouldIncrementAttemptCountOnFailure()
    {
        // Arrange
        _grid.SetWalkable(10, 5, false);
        var entity = CreateAgentWithRequest(5, 5, 10, 5);

        // Act
        _system.BeforeUpdate(1);
        _system.Update(1);

        // Assert
        var state = _world.Get<PathState>(entity);
        Assert.Equal(1, state.AttemptCount);
    }

    [Fact]
    public void Update_SuccessfulPath_ShouldClearFailReason()
    {
        // Arrange
        var entity = CreateAgentWithRequest(5, 5, 10, 5);
        ref var state = ref _world.Get<PathState>(entity);
        state.FailReason = PathFailReason.NoPathExists; // Tinha falhado antes

        // Act
        _system.BeforeUpdate(1);
        _system.Update(1);

        // Assert
        state = ref _world.Get<PathState>(entity);
        Assert.Equal(PathFailReason.None, state.FailReason);
    }

    [Fact]
    public void Update_ShouldUpdateLastUpdateTick()
    {
        // Arrange
        var entity = CreateAgentWithRequest(5, 5, 10, 5);

        // Act
        _system.BeforeUpdate(100);
        _system.Update(100);

        // Assert
        var state = _world.Get<PathState>(entity);
        Assert.Equal(100, state.LastUpdateTick);
    }

    [Fact]
    public void Update_MultipleRequests_ShouldProcessAll()
    {
        // Arrange
        var entity1 = CreateAgentWithRequest(5, 5, 10, 5);
        var entity2 = CreateAgentWithRequest(20, 20, 25, 25);
        var entity3 = CreateAgentWithRequest(40, 40, 45, 45);

        // Act
        _system.BeforeUpdate(1);
        _system.Update(1);

        // Assert
        Assert.False(_world.Has<PathRequest>(entity1));
        Assert.False(_world.Has<PathRequest>(entity2));
        Assert.False(_world.Has<PathRequest>(entity3));
    }

    [Fact]
    public void Update_WithFlags_ShouldRespectFlags()
    {
        // Arrange
        var entity = _world.Create(
            new NavigationAgent(),
            new GridPosition(0, 0),
            new PathRequest 
            { 
                TargetX = 5, 
                TargetY = 5,
                Flags = PathRequestFlags.CardinalOnly 
            },
            new PathState { Status = PathStatus.Pending },
            new GridPathBuffer()
        );

        // Act
        _system.BeforeUpdate(1);
        _system.Update(1);

        // Assert
        var state = _world.Get<PathState>(entity);
        Assert.Equal(PathStatus.Ready, state.Status);
        
        // Path deve existir (cardinal-only ainda deve encontrar)
        var buffer = _world.Get<GridPathBuffer>(entity);
        Assert.True(buffer.IsValid);
    }

    [Fact]
    public void BeforeUpdate_ShouldResetProcessedCount()
    {
        // Arrange - Cria 60 requisições
        for (int i = 0; i < 60; i++)
        {
            CreateAgentWithRequest(i % 10, i / 10, 50, 50);
        }

        // Act - Primeiro tick
        _system.BeforeUpdate(1);
        _system.Update(1);
        
        // Segundo tick - deve processar mais 10
        _system.BeforeUpdate(2);
        _system.Update(2);

        // Assert - Todos devem estar processados agora
        var query = new QueryDescription().WithAll<PathState, NavigationAgent>();
        int allProcessed = 0;
        _world.Query(in query, (ref PathState state) =>
        {
            if (state.Status == PathStatus.Ready || state.Status == PathStatus.Failed)
                allProcessed++;
        });

        Assert.Equal(60, allProcessed);
    }

    [Fact]
    public void Update_SamePositionRequest_ShouldSucceed()
    {
        // Arrange - Requisição para mesma posição
        var entity = CreateAgentWithRequest(5, 5, 5, 5);

        // Act
        _system.BeforeUpdate(1);
        _system.Update(1);

        // Assert
        var state = _world.Get<PathState>(entity);
        Assert.Equal(PathStatus.Ready, state.Status);
    }

    [Fact]
    public void Update_OutOfBoundsTarget_ShouldFail()
    {
        // Arrange
        var entity = CreateAgentWithRequest(5, 5, 200, 200); // Fora do grid

        // Act
        _system.BeforeUpdate(1);
        _system.Update(1);

        // Assert
        var state = _world.Get<PathState>(entity);
        Assert.Equal(PathStatus.Failed, state.Status);
    }
}

#endregion

#region Integration Tests

public class ServerSystemsIntegrationTests : IDisposable
{
    private readonly World _world;
    private readonly NavigationGrid _grid;
    private readonly PathfindingPool _pool;
    private readonly PathfindingService _pathfinder;
    private readonly ServerPathRequestSystem _pathRequestSystem;
    private readonly ServerMovementSystem _movementSystem;

    public ServerSystemsIntegrationTests()
    {
        _world = World.Create();
        _grid = new NavigationGrid(100, 100);
        _pool = new PathfindingPool(_grid.TotalCells);
        _pathfinder = new PathfindingService(_grid, _pool);
        _pathRequestSystem = new ServerPathRequestSystem(_world, _pathfinder);
        _movementSystem = new ServerMovementSystem(_world, _grid);
    }

    public void Dispose()
    {
        _world.Dispose();
    }

    private Entity CreateAgent(int x, int y)
    {
        var entity = _world.Create(
            new NavigationAgent(),
            new GridPosition(x, y),
            new ServerMovement(),
            new GridPathBuffer(),
            new PathState(),
            new ServerAgentConfig 
            { 
                CardinalMoveTicks = 6, 
                DiagonalMoveTicks = 9,
                AllowDiagonal = true,
                MaxRetries = 3 
            }
        );
        _grid.TryOccupy(new GridPosition(x, y), entity.Id);
        return entity;
    }

    [Fact]
    public void FullPathfindingCycle_ShouldWork()
    {
        // Arrange
        var entity = CreateAgent(5, 5);
        
        // Adiciona requisição de path
        _world.Add(entity, new PathRequest { TargetX = 10, TargetY = 5 });
        _world.Get<PathState>(entity).Status = PathStatus.Pending;

        // Act - Processa requisição de path
        _pathRequestSystem.BeforeUpdate(1);
        _pathRequestSystem.Update(1);

        // Assert - Path encontrado
        var state = _world.Get<PathState>(entity);
        Assert.Equal(PathStatus.Ready, state.Status);

        // Act - Simula múltiplos ticks de movimento
        for (long tick = 1; tick <= 50; tick++)
        {
            _movementSystem.Update(tick);
        }

        // Assert - Deve ter chegado ao destino
        var pos = _world.Get<GridPosition>(entity);
        Assert.Equal(10, pos.X);
        Assert.Equal(5, pos.Y);
        Assert.True(_world.Has<ReachedDestination>(entity));
    }

    [Fact]
    public void MultipleAgents_ShouldNavigateIndependently()
    {
        // Arrange
        var agent1 = CreateAgent(0, 0);
        var agent2 = CreateAgent(0, 50);

        // Adiciona requisições
        _world.Add(agent1, new PathRequest { TargetX = 10, TargetY = 0 });
        _world.Get<PathState>(agent1).Status = PathStatus.Pending;
        
        _world.Add(agent2, new PathRequest { TargetX = 10, TargetY = 50 });
        _world.Get<PathState>(agent2).Status = PathStatus.Pending;

        // Processa paths
        _pathRequestSystem.BeforeUpdate(1);
        _pathRequestSystem.Update(1);

        // Simula movimento
        for (long tick = 1; tick <= 100; tick++)
        {
            _movementSystem.Update(tick);
        }

        // Assert
        Assert.Equal(10, _world.Get<GridPosition>(agent1).X);
        Assert.Equal(10, _world.Get<GridPosition>(agent2).X);
    }

    [Fact]
    public void AgentBlocking_ShouldWait()
    {
        // Arrange - Dois agentes em linha
        var blocker = CreateAgent(6, 5);
        var mover = CreateAgent(5, 5);

        // Mover quer ir para (7, 5) mas blocker está em (6, 5)
        _world.Add(mover, new PathRequest { TargetX = 7, TargetY = 5 });
        _world.Get<PathState>(mover).Status = PathStatus.Pending;

        // Processa path
        _pathRequestSystem.BeforeUpdate(1);
        _pathRequestSystem.Update(1);

        // Tenta mover - deve ser bloqueado
        _movementSystem.Update(1);

        // Assert
        Assert.True(_world.Has<WaitingForPath>(mover));
        Assert.Equal(5, _world.Get<GridPosition>(mover).X); // Não moveu
    }
}

#endregion
