using Arch.Core;
using Game.ECS.Navigation.Server;
using Game.ECS.Navigation.Server.Components;
using Game.ECS.Navigation.Shared.Components;
using Game.ECS.Navigation.Shared.Data;

namespace Game.ECS.Navigation.Tests.Server;

/// <summary>
/// Testes para ServerNavigationModule e sistemas do servidor.
/// </summary>
public class ServerNavigationModuleTests : IDisposable
{
    private readonly World _world;
    private readonly ServerNavigationModule _module;

    public ServerNavigationModuleTests()
    {
        _world = World.Create();
        _module = new ServerNavigationModule(_world, 50, 50);
    }

    public void Dispose()
    {
        _module.Dispose();
        _world.Dispose();
    }

    #region Module Initialization Tests

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        Assert.NotNull(_module.Grid);
        Assert.NotNull(_module.Pathfinder);
        Assert.NotNull(_module.Config);
        Assert.Equal(50, _module.Grid.Width);
        Assert.Equal(50, _module.Grid.Height);
    }

    [Fact]
    public void Constructor_WithCustomConfig_ShouldUseConfig()
    {
        using var world = World.Create();
        var config = new NavigationConfig { MaxNodesPerSearch = 500 };
        using var module = new ServerNavigationModule(world, 20, 20, config);

        Assert.Equal(500, module.Config.MaxNodesPerSearch);
    }

    #endregion

    #region Agent Creation Tests

    [Fact]
    public void CreateAgent_ShouldCreateEntityWithComponents()
    {
        // Act
        var entity = _module.CreateAgent(new GridPosition(10, 10));

        // Assert
        Assert.True(_world.IsAlive(entity));
        Assert.True(_world.Has<GridPosition>(entity));
        Assert.True(_world.Has<ServerMovement>(entity));
        Assert.True(_world.Has<GridPathBuffer>(entity));
        Assert.True(_world.Has<PathState>(entity));
        Assert.True(_world.Has<ServerAgentConfig>(entity));
        Assert.True(_world.Has<NavigationAgent>(entity));
    }

    [Fact]
    public void CreateAgent_ShouldSetPosition()
    {
        // Act
        var entity = _module.CreateAgent(new GridPosition(15, 25));

        // Assert
        var pos = _world.Get<GridPosition>(entity);
        Assert.Equal(15, pos.X);
        Assert.Equal(25, pos.Y);
    }

    [Fact]
    public void CreateAgent_ShouldOccupyCell()
    {
        // Act
        var pos = new GridPosition(10, 10);
        var entity = _module.CreateAgent(pos);

        // Assert
        Assert.True(_module.Grid.IsOccupied(10, 10));
        Assert.Equal(entity.Id, _module.Grid.GetOccupant(10, 10));
    }

    [Fact]
    public void CreateAgent_WithCustomConfig_ShouldUseConfig()
    {
        // Act
        var config = ServerAgentConfig.Fast;
        var entity = _module.CreateAgent(new GridPosition(5, 5), config);

        // Assert
        var agentConfig = _world.Get<ServerAgentConfig>(entity);
        Assert.Equal(ServerAgentConfig.Fast.CardinalMoveTicks, agentConfig.CardinalMoveTicks);
    }

    #endregion

    #region Agent Removal Tests

    [Fact]
    public void RemoveAgent_ShouldDestroyEntity()
    {
        // Arrange
        var entity = _module.CreateAgent(new GridPosition(10, 10));

        // Act
        _module.RemoveAgent(entity);

        // Assert
        Assert.False(_world.IsAlive(entity));
    }

    [Fact]
    public void RemoveAgent_ShouldReleaseCell()
    {
        // Arrange
        var pos = new GridPosition(10, 10);
        var entity = _module.CreateAgent(pos);

        // Act
        _module.RemoveAgent(entity);

        // Assert
        Assert.False(_module.Grid.IsOccupied(10, 10));
    }

    #endregion

    #region Movement Request Tests

    [Fact]
    public void RequestMoveTo_ShouldSetPendingStatus()
    {
        // Arrange
        var entity = _module.CreateAgent(new GridPosition(5, 5));

        // Act
        _module.RequestMoveTo(entity, new GridPosition(10, 10));

        // Assert
        var state = _world.Get<PathState>(entity);
        Assert.Equal(PathStatus.Pending, state.Status);
    }

    [Fact]
    public void RequestMoveTo_ShouldAddPathRequest()
    {
        // Arrange
        var entity = _module.CreateAgent(new GridPosition(5, 5));

        // Act
        _module.RequestMoveTo(entity, new GridPosition(10, 10));

        // Assert
        Assert.True(_world.Has<PathRequest>(entity));
    }

    [Fact]
    public void RequestMoveTo_InvalidTarget_ShouldNotAddRequest()
    {
        // Arrange
        var entity = _module.CreateAgent(new GridPosition(5, 5));

        // Act
        _module.RequestMoveTo(entity, new GridPosition(-1, -1));

        // Assert
        Assert.False(_world.Has<PathRequest>(entity));
    }

    [Fact]
    public void RequestMoveTo_ShouldClearPreviousPath()
    {
        // Arrange
        var entity = _module.CreateAgent(new GridPosition(5, 5));
        _module.RequestMoveTo(entity, new GridPosition(10, 10));
        _module.Tick(1); // Process first request

        // Act
        _module.RequestMoveTo(entity, new GridPosition(15, 15));

        // Assert
        var buffer = _world.Get<GridPathBuffer>(entity);
        Assert.Equal(0, buffer.WaypointCount);
    }

    #endregion

    #region Stop Agent Tests

    [Fact]
    public void StopAgent_ShouldCancelPath()
    {
        // Arrange
        var entity = _module.CreateAgent(new GridPosition(5, 5));
        _module.RequestMoveTo(entity, new GridPosition(10, 10));

        // Act
        _module.StopAgent(entity);

        // Assert
        var state = _world.Get<PathState>(entity);
        Assert.Equal(PathStatus.Cancelled, state.Status);
        Assert.False(_world.Has<PathRequest>(entity));
    }

    [Fact]
    public void StopAgent_ShouldResetMovement()
    {
        // Arrange
        var entity = _module.CreateAgent(new GridPosition(5, 5));
        _module.RequestMoveTo(entity, new GridPosition(10, 10));
        _module.Tick(1); // Start movement

        // Act
        _module.StopAgent(entity);

        // Assert
        var movement = _world.Get<ServerMovement>(entity);
        Assert.False(movement.IsMoving);
    }

    #endregion

    #region Snapshot Tests

    [Fact]
    public void GetSnapshot_StationaryAgent_ShouldReturnCorrectData()
    {
        // Arrange
        var entity = _module.CreateAgent(new GridPosition(10, 15));

        // Act
        var snapshot = _module.GetSnapshot(entity, currentTick: 100);

        // Assert
        Assert.Equal(entity.Id, snapshot.EntityId);
        Assert.Equal(10, snapshot.CurrentX);
        Assert.Equal(15, snapshot.CurrentY);
        Assert.False(snapshot.IsMoving);
        Assert.Equal(0, snapshot.TicksRemaining);
    }

    [Fact]
    public void GetSnapshot_MovingAgent_ShouldIncludeTarget()
    {
        // Arrange
        var entity = _module.CreateAgent(new GridPosition(5, 5));
        _module.RequestMoveTo(entity, new GridPosition(10, 10));
        _module.Tick(1); // Process path
        _module.Tick(2); // Start movement

        // Act
        var snapshot = _module.GetSnapshot(entity, currentTick: 3);

        // Assert
        Assert.True(snapshot.IsMoving);
        Assert.True(snapshot.TicksRemaining > 0);
    }

    #endregion

    #region Tick Processing Tests

    [Fact]
    public void Tick_ShouldProcessPathRequests()
    {
        // Arrange
        var entity = _module.CreateAgent(new GridPosition(5, 5));
        _module.RequestMoveTo(entity, new GridPosition(10, 5));

        // Act
        _module.Tick(1);

        // Assert
        var state = _world.Get<PathState>(entity);
        // After processing, should be Ready, Following, or Failed (never Pending or Computing)
        Assert.True(state.Status == PathStatus.Ready || 
                    state.Status == PathStatus.Failed ||
                    state.Status == PathStatus.Following);
        Assert.False(_world.Has<PathRequest>(entity));
    }

    [Fact]
    public void Tick_ShouldStartMovement()
    {
        // Arrange
        var entity = _module.CreateAgent(new GridPosition(5, 5));
        _module.RequestMoveTo(entity, new GridPosition(6, 5));
        _module.Tick(1); // Process path

        // Act
        _module.Tick(2); // Should start movement

        // Assert
        var movement = _world.Get<ServerMovement>(entity);
        Assert.True(movement.IsMoving);
    }

    [Fact]
    public void Tick_ShouldCompleteMovement()
    {
        // Arrange
        var entity = _module.CreateAgent(new GridPosition(5, 5));
        _module.RequestMoveTo(entity, new GridPosition(6, 5));
        _module.Tick(1); // Process path
        _module.Tick(2); // Start movement

        var config = _world.Get<ServerAgentConfig>(entity);
        
        // Act - Tick enough times to complete movement
        for (int i = 3; i <= 3 + config.CardinalMoveTicks; i++)
        {
            _module.Tick(i);
        }

        // Assert
        var pos = _world.Get<GridPosition>(entity);
        Assert.Equal(6, pos.X);
        Assert.Equal(5, pos.Y);
    }

    [Fact]
    public void Tick_FullPath_ShouldReachDestination()
    {
        // Arrange
        var entity = _module.CreateAgent(new GridPosition(5, 5));
        _module.RequestMoveTo(entity, new GridPosition(8, 5)); // 3 cells away

        // Act - Tick many times
        for (int i = 1; i <= 100; i++)
        {
            _module.Tick(i);
        }

        // Assert
        var state = _world.Get<PathState>(entity);
        var pos = _world.Get<GridPosition>(entity);
        Assert.Equal(PathStatus.Completed, state.Status);
        Assert.Equal(8, pos.X);
        Assert.Equal(5, pos.Y);
        Assert.True(_world.Has<ReachedDestination>(entity));
    }

    #endregion

    #region Collision/Blocking Tests

    [Fact]
    public void Movement_BlockedByObstacle_ShouldFail()
    {
        // Arrange
        // Create wall between agent and target
        for (int y = 0; y < 50; y++)
        {
            _module.Grid.SetWalkable(10, y, false);
        }
        
        var entity = _module.CreateAgent(new GridPosition(5, 5));
        _module.RequestMoveTo(entity, new GridPosition(15, 5));

        // Act
        _module.Tick(1);

        // Assert
        var state = _world.Get<PathState>(entity);
        Assert.Equal(PathStatus.Failed, state.Status);
        Assert.Equal(PathFailReason.NoPathExists, state.FailReason);
    }

    [Fact]
    public void Movement_BlockedByOtherAgent_ShouldWait()
    {
        // Arrange
        var agent1 = _module.CreateAgent(new GridPosition(5, 5));
        var agent2 = _module.CreateAgent(new GridPosition(6, 5));
        
        // Agent 1 tries to move to where Agent 2 is
        _module.RequestMoveTo(agent1, new GridPosition(6, 5));
        _module.Tick(1); // Process path

        // Act
        _module.Tick(2);

        // Assert - Agent should be waiting
        var pos1 = _world.Get<GridPosition>(agent1);
        Assert.Equal(5, pos1.X); // Hasn't moved
        Assert.True(_world.Has<WaitingForPath>(agent1));
    }

    #endregion
}

/// <summary>
/// Testes para ServerAgentConfig.
/// </summary>
public class ServerAgentConfigTests
{
    [Fact]
    public void Default_ShouldHaveReasonableValues()
    {
        var config = ServerAgentConfig.Default;

        Assert.True(config.CardinalMoveTicks > 0);
        Assert.True(config.DiagonalMoveTicks > config.CardinalMoveTicks);
        Assert.True(config.AllowDiagonal);
        Assert.True(config.MaxRetries > 0);
    }

    [Fact]
    public void GetMoveTicks_ShouldReturnCorrectValue()
    {
        var config = ServerAgentConfig.Default;

        Assert.Equal(config.CardinalMoveTicks, config.GetMoveTicks(diagonal: false));
        Assert.Equal(config.DiagonalMoveTicks, config.GetMoveTicks(diagonal: true));
    }

    [Fact]
    public void Fast_ShouldBeFasterThanDefault()
    {
        var fast = ServerAgentConfig.Fast;
        var def = ServerAgentConfig.Default;

        Assert.True(fast.CardinalMoveTicks < def.CardinalMoveTicks);
    }

    [Fact]
    public void Slow_ShouldBeSlowerThanDefault()
    {
        var slow = ServerAgentConfig.Slow;
        var def = ServerAgentConfig.Default;

        Assert.True(slow.CardinalMoveTicks > def.CardinalMoveTicks);
    }
}

/// <summary>
/// Testes para ServerMovement.
/// </summary>
public class ServerMovementTests
{
    [Fact]
    public void Start_ShouldSetMovingState()
    {
        // Arrange
        var movement = new ServerMovement();
        var from = new GridPosition(5, 5);
        var to = new GridPosition(6, 5);

        // Act
        movement.Start(from, to, currentTick: 10, durationTicks: 6);

        // Assert
        Assert.True(movement.IsMoving);
        Assert.Equal(to, movement.TargetCell);
        Assert.Equal(10, movement.StartTick);
        Assert.Equal(16, movement.EndTick);
        Assert.Equal(MovementDirection.East, movement.Direction);
    }

    [Fact]
    public void ShouldComplete_BeforeEndTick_ShouldReturnFalse()
    {
        // Arrange
        var movement = new ServerMovement();
        movement.Start(new GridPosition(0, 0), new GridPosition(1, 0), 10, 6);

        // Assert
        Assert.False(movement.ShouldComplete(10));
        Assert.False(movement.ShouldComplete(15));
    }

    [Fact]
    public void ShouldComplete_AtEndTick_ShouldReturnTrue()
    {
        // Arrange
        var movement = new ServerMovement();
        movement.Start(new GridPosition(0, 0), new GridPosition(1, 0), 10, 6);

        // Assert
        Assert.True(movement.ShouldComplete(16));
        Assert.True(movement.ShouldComplete(20));
    }

    [Fact]
    public void Complete_ShouldStopMoving()
    {
        // Arrange
        var movement = new ServerMovement();
        movement.Start(new GridPosition(0, 0), new GridPosition(1, 0), 10, 6);

        // Act
        movement.Complete();

        // Assert
        Assert.False(movement.IsMoving);
    }

    [Fact]
    public void Reset_ShouldStopMoving()
    {
        // Arrange
        var movement = new ServerMovement();
        movement.Start(new GridPosition(0, 0), new GridPosition(1, 0), 10, 6);

        // Act
        movement.Reset();

        // Assert
        Assert.False(movement.IsMoving);
    }
}
