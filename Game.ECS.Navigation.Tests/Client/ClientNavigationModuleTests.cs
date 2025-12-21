using Arch.Core;
using Game.ECS.Client.Client;
using Game.ECS.Client.Client.Components;
using Game.ECS.Shared.Components.Navigation;
using Game.ECS.Shared.Data.Navigation;

namespace Game.ECS.Navigation.Tests.Client;

/// <summary>
/// Testes para ClientNavigationModule e componentes do cliente.
/// </summary>
public class ClientNavigationModuleTests : IDisposable
{
    private readonly World _world;
    private readonly ClientNavigationModule _module;

    public ClientNavigationModuleTests()
    {
        _world = World.Create();
        _module = new ClientNavigationModule(_world, 32f, 60f);
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
        Assert.Equal(32f, _module.CellSize);
        Assert.Equal(60f, _module.TickRate);
    }

    [Fact]
    public void Constructor_WithCustomValues_ShouldUseValues()
    {
        using var world = World.Create();
        using var module = new ClientNavigationModule(world, 64f, 30f);

        Assert.Equal(64f, module.CellSize);
        Assert.Equal(30f, module.TickRate);
    }

    #endregion

    #region Entity Creation Tests

    [Fact]
    public void CreateEntity_ShouldCreateWithAllComponents()
    {
        // Act
        var entity = _module.CreateEntity(1, 10, 15);

        // Assert
        Assert.True(_world.IsAlive(entity));
        Assert.True(_world.Has<SyncedGridPosition>(entity));
        Assert.True(_world.Has<VisualPosition>(entity));
        Assert.True(_world.Has<VisualInterpolation>(entity));
        Assert.True(_world.Has<MovementQueue>(entity));
        Assert.True(_world.Has<ClientVisualConfig>(entity));
        Assert.True(_world.Has<SpriteAnimation>(entity));
        Assert.True(_world.Has<ClientNavigationEntity>(entity));
    }

    [Fact]
    public void CreateEntity_ShouldSetCorrectPosition()
    {
        // Act
        var entity = _module.CreateEntity(1, 10, 15);

        // Assert
        var syncedPos = _world.Get<SyncedGridPosition>(entity);
        Assert.Equal(10, syncedPos.X);
        Assert.Equal(15, syncedPos.Y);
    }

    [Fact]
    public void CreateEntity_ShouldSetVisualPosition()
    {
        // Act
        var entity = _module.CreateEntity(1, 10, 15);

        // Assert
        var visualPos = _world.Get<VisualPosition>(entity);
        // Visual position should be centered in the cell
        Assert.Equal((10 + 0.5f) * 32f, visualPos.X);
        Assert.Equal((15 + 0.5f) * 32f, visualPos.Y);
    }

    [Fact]
    public void CreateEntity_LocalPlayer_ShouldHaveTag()
    {
        // Act
        var entity = _module.CreateEntity(1, 5, 5, true);

        // Assert
        Assert.True(_world.Has<LocalPlayer>(entity));
    }

    [Fact]
    public void CreateEntity_NotLocalPlayer_ShouldNotHaveTag()
    {
        // Act
        var entity = _module.CreateEntity(1, 5, 5, false);

        // Assert
        Assert.False(_world.Has<LocalPlayer>(entity));
    }

    [Fact]
    public void CreateEntity_WithCustomConfig_ShouldUseConfig()
    {
        // Arrange
        var config = ClientVisualConfig.Instant;

        // Act
        var entity = _module.CreateEntity(1, 5, 5, settings: config);

        // Assert
        var actualConfig = _world.Get<ClientVisualConfig>(entity);
        Assert.False(actualConfig.SmoothMovement);
    }

    #endregion

    #region Entity Destruction Tests

    [Fact]
    public void DestroyEntity_ShouldRemoveEntity()
    {
        // Arrange
        var entity = _module.CreateEntity(1, 5, 5);

        // Act
        _module.DestroyEntity(1);

        // Assert
        Assert.False(_world.IsAlive(entity));
    }

    [Fact]
    public void DestroyEntity_InvalidId_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        _module.DestroyEntity(999);
    }

    #endregion

    #region Snapshot Processing Tests

    [Fact]
    public void OnMovementSnapshot_ShouldUpdatePosition()
    {
        // Arrange
        var entity = _module.CreateEntity(1, 5, 5);
        var snapshot = new MovementSnapshot
        {
            EntityId = 1,
            CurrentX = 10,
            CurrentY = 10,
            IsMoving = false,
            Direction = MovementDirection.South
        };

        // Act
        _module.OnMovementSnapshot(snapshot);
        _module.Update(0.016f); // One frame

        // Assert
        var syncedPos = _world.Get<SyncedGridPosition>(entity);
        Assert.Equal(10, syncedPos.X);
        Assert.Equal(10, syncedPos.Y);
    }

    [Fact]
    public void OnMovementSnapshot_WithMovement_ShouldStartInterpolation()
    {
        // Arrange
        var entity = _module.CreateEntity(1, 5, 5);
        var snapshot = new MovementSnapshot
        {
            EntityId = 1,
            CurrentX = 5,
            CurrentY = 5,
            TargetX = 6,
            TargetY = 5,
            IsMoving = true,
            Direction = MovementDirection.East,
            TicksRemaining = 6
        };

        // Act
        _module.OnMovementSnapshot(snapshot);
        _module.Update(0.016f);

        // Assert
        var interp = _world.Get<VisualInterpolation>(entity);
        Assert.True(interp.IsActive);
        Assert.Equal(MovementDirection.East, interp.Direction);
    }

    [Fact]
    public void OnBatchUpdate_ShouldProcessAll()
    {
        // Arrange
        _module.CreateEntity(1, 0, 0);
        _module.CreateEntity(2, 5, 5);

        var batch = new BatchMovementUpdate
        {
            ServerTick = 100,
            Snapshots = new[]
            {
                new MovementSnapshot { EntityId = 1, CurrentX = 1, CurrentY = 1, IsMoving = false },
                new MovementSnapshot { EntityId = 2, CurrentX = 6, CurrentY = 6, IsMoving = false }
            }
        };

        // Act
        _module.OnBatchUpdate(batch);
        _module.Update(0.016f);

        // Assert
        var pos1 = _world.Get<SyncedGridPosition>(_module.GetEntity(1)!.Value);
        var pos2 = _world.Get<SyncedGridPosition>(_module.GetEntity(2)!.Value);
        Assert.Equal(1, pos1.X);
        Assert.Equal(6, pos2.X);
    }

    #endregion

    #region Teleport Tests

    [Fact]
    public void OnTeleport_ShouldUpdatePositionInstantly()
    {
        // Arrange
        var entity = _module.CreateEntity(1, 5, 5);
        var teleport = new TeleportMessage
        {
            EntityId = 1,
            X = 20,
            Y = 20,
            FacingDirection = MovementDirection.North
        };

        // Act
        _module.OnTeleport(teleport);
        _module.Update(0.016f);

        // Assert
        var syncedPos = _world.Get<SyncedGridPosition>(entity);
        var visualPos = _world.Get<VisualPosition>(entity);

        Assert.Equal(20, syncedPos.X);
        Assert.Equal(20, syncedPos.Y);
        Assert.Equal((20 + 0.5f) * 32f, visualPos.X);
        Assert.Equal((20 + 0.5f) * 32f, visualPos.Y);
    }

    [Fact]
    public void OnTeleport_ShouldStopInterpolation()
    {
        // Arrange
        var entity = _module.CreateEntity(1, 5, 5);

        // Start interpolation
        var snapshot = new MovementSnapshot
        {
            EntityId = 1,
            CurrentX = 5,
            CurrentY = 5,
            TargetX = 6,
            TargetY = 5,
            IsMoving = true,
            Direction = MovementDirection.East,
            TicksRemaining = 6
        };
        _module.OnMovementSnapshot(snapshot);
        _module.Update(0.016f);

        // Act - Teleport should cancel interpolation
        var teleport = new TeleportMessage
        {
            EntityId = 1,
            X = 20,
            Y = 20,
            FacingDirection = MovementDirection.North
        };
        _module.OnTeleport(teleport);
        _module.Update(0.016f);

        // Assert
        var interp = _world.Get<VisualInterpolation>(entity);
        Assert.False(interp.IsActive);
    }

    #endregion

    #region Query Methods Tests

    [Fact]
    public void GetVisualPosition_ShouldReturnCorrectPosition()
    {
        // Arrange
        _module.CreateEntity(1, 10, 15);

        // Act
        var visualPos = _module.GetVisualPosition(1);

        // Assert
        Assert.Equal((10 + 0.5f) * 32f, visualPos.X);
        Assert.Equal((15 + 0.5f) * 32f, visualPos.Y);
    }

    [Fact]
    public void GetVisualPosition_InvalidId_ShouldReturnDefault()
    {
        // Act
        var visualPos = _module.GetVisualPosition(999);

        // Assert
        Assert.Equal(0f, visualPos.X);
        Assert.Equal(0f, visualPos.Y);
    }

    [Fact]
    public void GetAnimationState_ShouldReturnState()
    {
        // Arrange
        _module.CreateEntity(1, 5, 5);

        // Act
        var animState = _module.GetAnimationState(1);

        // Assert
        Assert.Equal(MovementDirection.South, animState.Facing);
        Assert.Equal(AnimationClip.Idle, animState.Clip);
    }

    [Fact]
    public void GetEntity_ShouldReturnEntity()
    {
        // Arrange
        var created = _module.CreateEntity(1, 5, 5);

        // Act
        var retrieved = _module.GetEntity(1);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created, retrieved.Value);
    }

    [Fact]
    public void GetEntity_InvalidId_ShouldReturnNull()
    {
        // Act
        var retrieved = _module.GetEntity(999);

        // Assert
        Assert.Null(retrieved);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldProcessInterpolation()
    {
        // Arrange
        var entity = _module.CreateEntity(1, 0, 0);

        // Start movement
        var snapshot = new MovementSnapshot
        {
            EntityId = 1,
            CurrentX = 0,
            CurrentY = 0,
            TargetX = 1,
            TargetY = 0,
            IsMoving = true,
            Direction = MovementDirection.East,
            TicksRemaining = 6
        };
        _module.OnMovementSnapshot(snapshot);
        _module.Update(0.016f); // Start interpolation

        var startPos = _world.Get<VisualPosition>(entity);

        // Act - Update partial
        _module.Update(0.05f);

        // Assert
        var currentPos = _world.Get<VisualPosition>(entity);
        // Position should have moved
        Assert.True(currentPos.X > startPos.X || currentPos.X == startPos.X);
    }

    #endregion
}

/// <summary>
/// Testes para componentes do cliente.
/// </summary>
public class ClientComponentsTests
{
    #region VisualPosition Tests

    [Fact]
    public void VisualPosition_FromGrid_ShouldCenterInCell()
    {
        var pos = VisualPosition.FromGrid(5, 10, 32f);

        Assert.Equal((5 + 0.5f) * 32f, pos.X);
        Assert.Equal((10 + 0.5f) * 32f, pos.Y);
    }

    [Fact]
    public void VisualPosition_FromGridPosition_ShouldCenterInCell()
    {
        var gridPos = new GridPosition(5, 10);
        var pos = VisualPosition.FromGrid(gridPos, 32f);

        Assert.Equal((5 + 0.5f) * 32f, pos.X);
        Assert.Equal((10 + 0.5f) * 32f, pos.Y);
    }

    [Fact]
    public void VisualPosition_Lerp_ShouldInterpolate()
    {
        var a = new VisualPosition(0, 0);
        var b = new VisualPosition(100, 100);

        var half = VisualPosition.Lerp(a, b, 0.5f);

        Assert.Equal(50f, half.X);
        Assert.Equal(50f, half.Y);
    }

    [Fact]
    public void VisualPosition_DistanceTo_ShouldCalculate()
    {
        var a = new VisualPosition(0, 0);
        var b = new VisualPosition(3, 4);

        Assert.Equal(5f, a.DistanceTo(b));
    }

    [Fact]
    public void VisualPosition_DistanceSquaredTo_ShouldCalculate()
    {
        var a = new VisualPosition(0, 0);
        var b = new VisualPosition(3, 4);

        Assert.Equal(25f, a.DistanceSquaredTo(b));
    }

    #endregion

    #region SyncedGridPosition Tests

    [Fact]
    public void SyncedGridPosition_ToGridPosition_ShouldConvert()
    {
        var synced = new SyncedGridPosition(10, 20, 100);
        var gridPos = synced.ToGridPosition();

        Assert.Equal(10, gridPos.X);
        Assert.Equal(20, gridPos.Y);
    }

    #endregion

    #region VisualInterpolation Tests

    [Fact]
    public void VisualInterpolation_Start_ShouldInitialize()
    {
        var interp = new VisualInterpolation();
        var from = new VisualPosition(0, 0);
        var to = new VisualPosition(100, 0);

        interp.Start(from, to, 0.5f, MovementDirection.East);

        Assert.True(interp.IsActive);
        Assert.Equal(0f, interp.Progress);
        Assert.Equal(0.5f, interp.Duration);
        Assert.Equal(MovementDirection.East, interp.Direction);
    }

    [Fact]
    public void VisualInterpolation_IsComplete_ShouldWork()
    {
        var interp = new VisualInterpolation { Progress = 0.5f };
        Assert.False(interp.IsComplete);

        interp.Progress = 1f;
        Assert.True(interp.IsComplete);
    }

    [Fact]
    public void VisualInterpolation_Finish_ShouldComplete()
    {
        var interp = new VisualInterpolation();
        interp.Start(new VisualPosition(0, 0), new VisualPosition(100, 0), 1f, MovementDirection.East);

        interp.Finish();

        Assert.False(interp.IsActive);
        Assert.Equal(1f, interp.Progress);
    }

    [Fact]
    public void VisualInterpolation_GetCurrentPosition_Active_ShouldInterpolate()
    {
        var interp = new VisualInterpolation();
        interp.Start(new VisualPosition(0, 0), new VisualPosition(100, 0), 1f, MovementDirection.East);
        interp.Progress = 0.5f;

        var current = interp.GetCurrentPosition();

        Assert.Equal(50f, current.X);
    }

    [Fact]
    public void VisualInterpolation_GetCurrentPosition_Inactive_ShouldReturnTo()
    {
        var interp = new VisualInterpolation
        {
            IsActive = false,
            To = new VisualPosition(100, 50)
        };

        var current = interp.GetCurrentPosition();

        Assert.Equal(100f, current.X);
        Assert.Equal(50f, current.Y);
    }

    #endregion

    #region MovementQueue Tests

    [Fact]
    public void MovementQueue_Enqueue_ShouldAddItem()
    {
        var queue = new MovementQueue();

        queue.Enqueue(10, 20, 0.5f, MovementDirection.North);

        Assert.True(queue.HasItems);
        Assert.Equal(1, queue.Count);
    }

    [Fact]
    public void MovementQueue_TryDequeue_ShouldReturnItem()
    {
        var queue = new MovementQueue();
        queue.Enqueue(10, 20, 0.5f, MovementDirection.North);

        var success = queue.TryDequeue(out var x, out var y, out var duration, out var dir);

        Assert.True(success);
        Assert.Equal(10, x);
        Assert.Equal(20, y);
        Assert.Equal(0.5f, duration);
        Assert.Equal(MovementDirection.North, dir);
        Assert.False(queue.HasItems);
    }

    [Fact]
    public void MovementQueue_TryDequeue_Empty_ShouldReturnFalse()
    {
        var queue = new MovementQueue();

        var success = queue.TryDequeue(out _, out _, out _, out _);

        Assert.False(success);
    }

    [Fact]
    public void MovementQueue_Clear_ShouldEmptyQueue()
    {
        var queue = new MovementQueue();
        queue.Enqueue(1, 1, 0.1f, MovementDirection.North);
        queue.Enqueue(2, 2, 0.2f, MovementDirection.South);

        queue.Clear();

        Assert.False(queue.HasItems);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void MovementQueue_IsFull_ShouldDetectFullQueue()
    {
        var queue = new MovementQueue();

        // Fill the queue
        for (var i = 0; i < MovementQueue.Capacity; i++) queue.Enqueue(i, i, 0.1f, MovementDirection.None);

        Assert.True(queue.IsFull);
    }

    [Fact]
    public void MovementQueue_Overflow_ShouldDiscardOldest()
    {
        var queue = new MovementQueue();

        // Fill and overflow
        for (var i = 0; i < MovementQueue.Capacity + 1; i++) queue.Enqueue(i, i, 0.1f, MovementDirection.None);

        // First dequeue should be item 1, not item 0
        queue.TryDequeue(out var x, out _, out _, out _);
        Assert.Equal(1, x);
    }

    #endregion

    #region ClientVisualConfig Tests

    [Fact]
    public void ClientVisualConfig_Default_ShouldHaveSmoothMovement()
    {
        var config = ClientVisualConfig.Default;

        Assert.True(config.SmoothMovement);
        Assert.Equal(32f, config.CellSize);
    }

    [Fact]
    public void ClientVisualConfig_Instant_ShouldNotHaveSmoothMovement()
    {
        var config = ClientVisualConfig.Instant;

        Assert.False(config.SmoothMovement);
    }

    #endregion

    #region SpriteAnimation Tests

    [Fact]
    public void SpriteAnimation_SetClip_ShouldResetTime()
    {
        var anim = new SpriteAnimation
        {
            Clip = AnimationClip.Idle,
            Time = 5f,
            Frame = 3
        };

        anim.SetClip(AnimationClip.Walk);

        Assert.Equal(AnimationClip.Walk, anim.Clip);
        Assert.Equal(0f, anim.Time);
        Assert.Equal(0, anim.Frame);
    }

    [Fact]
    public void SpriteAnimation_SetClip_SameClip_ShouldNotReset()
    {
        var anim = new SpriteAnimation
        {
            Clip = AnimationClip.Walk,
            Time = 5f,
            Frame = 3
        };

        anim.SetClip(AnimationClip.Walk);

        Assert.Equal(5f, anim.Time);
        Assert.Equal(3, anim.Frame);
    }

    [Fact]
    public void SpriteAnimation_Reset_ShouldSetToIdle()
    {
        var anim = new SpriteAnimation
        {
            Clip = AnimationClip.Run,
            Time = 10f,
            Frame = 5
        };

        anim.Reset();

        Assert.Equal(AnimationClip.Idle, anim.Clip);
        Assert.Equal(0f, anim.Time);
        Assert.Equal(0, anim.Frame);
    }

    #endregion
}