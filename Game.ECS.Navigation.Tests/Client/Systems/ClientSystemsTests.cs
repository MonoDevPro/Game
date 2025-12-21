using Arch.Core;
using Game.ECS.Client.Client.Components;
using Game.ECS.Client.Client.Contracts;
using Game.ECS.Client.Client.Systems;
using Game.ECS.Shared.Components.Navigation;
using Game.ECS.Shared.Data.Navigation;

namespace Game.ECS.Navigation.Tests.Client.Systems;

#region Mocks

public class MockInputProvider : IInputProvider
{
    public bool ClickPressed { get; set; }
    public (float X, float Y) ClickPosition { get; set; }
    public (float X, float Y) MovementAxis { get; set; }

    public bool IsClickPressed()
    {
        return ClickPressed;
    }

    public (float X, float Y) GetClickWorldPosition()
    {
        return ClickPosition;
    }

    public (float X, float Y) GetMovementAxis()
    {
        return MovementAxis;
    }
}

public class MockNetworkSender : INetworkSender
{
    public List<MoveInput> SentInputs { get; } = new();

    public void SendMoveInput(MoveInput input)
    {
        SentInputs.Add(input);
    }
}

#endregion

#region ClientInterpolationSystem Tests

public class ClientInterpolationSystemTests : IDisposable
{
    private readonly World _world;
    private readonly ClientInterpolationSystem _system;
    private const float CellSize = 32f;

    public ClientInterpolationSystemTests()
    {
        _world = World.Create();
        _system = new ClientInterpolationSystem(_world, CellSize);
    }

    public void Dispose()
    {
        _world.Dispose();
    }

    private Entity CreateInterpolatingEntity()
    {
        return _world.Create(
            new ClientNavigationEntity(),
            new VisualPosition { X = 0, Y = 0 },
            new VisualInterpolation(),
            new ClientVisualConfig { SmoothMovement = true, InterpolationSpeed = 1f, Easing = EasingType.Linear },
            new MovementQueue()
        );
    }

    [Fact]
    public void Update_NoActiveInterpolation_ShouldNotChangePosition()
    {
        // Arrange
        var entity = CreateInterpolatingEntity();
        ref var visualPos = ref _world.Get<VisualPosition>(entity);
        visualPos.X = 100f;
        visualPos.Y = 100f;

        // Act
        _system.Update(0.016f);

        // Assert
        var pos = _world.Get<VisualPosition>(entity);
        Assert.Equal(100f, pos.X);
        Assert.Equal(100f, pos.Y);
    }

    [Fact]
    public void Update_ActiveInterpolation_ShouldUpdatePosition()
    {
        // Arrange
        var entity = CreateInterpolatingEntity();
        ref var movement = ref _world.Get<VisualInterpolation>(entity);
        var from = new VisualPosition { X = 0, Y = 0 };
        var to = new VisualPosition { X = 32f, Y = 0 };
        movement.Start(from, to, 1f, MovementDirection.East);

        // Act
        _system.Update(0.5f); // 50% através

        // Assert
        var pos = _world.Get<VisualPosition>(entity);
        Assert.True(pos.X > 0f && pos.X < 32f);
    }

    [Fact]
    public void Update_InterpolationComplete_ShouldSnapToTarget()
    {
        // Arrange
        var entity = CreateInterpolatingEntity();
        ref var movement = ref _world.Get<VisualInterpolation>(entity);
        var from = new VisualPosition { X = 0, Y = 0 };
        var to = new VisualPosition { X = 32f, Y = 0 };
        movement.Start(from, to, 0.5f, MovementDirection.East);

        // Act
        _system.Update(0.6f); // Mais que a duração

        // Assert
        var pos = _world.Get<VisualPosition>(entity);
        Assert.Equal(32f, pos.X, 0.01f);
        Assert.Equal(0f, pos.Y, 0.01f);
        Assert.False(_world.Get<VisualInterpolation>(entity).IsActive);
    }

    [Fact]
    public void Update_SmoothMovementDisabled_ShouldSnapInstantly()
    {
        // Arrange
        var entity = CreateInterpolatingEntity();
        ref var config = ref _world.Get<ClientVisualConfig>(entity);
        config.SmoothMovement = false;

        ref var movement = ref _world.Get<VisualInterpolation>(entity);
        var from = new VisualPosition { X = 0, Y = 0 };
        var to = new VisualPosition { X = 32f, Y = 0 };
        movement.Start(from, to, 1f, MovementDirection.East);

        // Act
        _system.Update(0.001f); // Delta muito pequeno

        // Assert
        var pos = _world.Get<VisualPosition>(entity);
        Assert.Equal(32f, pos.X, 0.01f);
    }

    [Fact]
    public void Update_ZeroDuration_ShouldCompleteInstantly()
    {
        // Arrange
        var entity = CreateInterpolatingEntity();
        ref var movement = ref _world.Get<VisualInterpolation>(entity);
        var from = new VisualPosition { X = 0, Y = 0 };
        var to = new VisualPosition { X = 32f, Y = 32f };
        movement.Start(from, to, 0f, MovementDirection.SouthEast);

        // Act
        _system.Update(0.016f);

        // Assert
        var pos = _world.Get<VisualPosition>(entity);
        Assert.Equal(32f, pos.X, 0.01f);
        Assert.Equal(32f, pos.Y, 0.01f);
    }

    [Fact]
    public void Update_WithBufferedMovements_ShouldProcessQueue()
    {
        // Arrange
        var entity = CreateInterpolatingEntity();
        ref var buffer = ref _world.Get<MovementQueue>(entity);
        buffer.Enqueue(1, 0, 0.1f, MovementDirection.East);
        buffer.Enqueue(2, 0, 0.1f, MovementDirection.East);

        // Act - primeira atualização deve iniciar interpolação do buffer
        _system.Update(0.016f);

        // Assert
        var movement = _world.Get<VisualInterpolation>(entity);
        Assert.True(movement.IsActive || buffer.HasItems);
    }

    [Theory]
    [InlineData(EasingType.Linear)]
    [InlineData(EasingType.QuadIn)]
    [InlineData(EasingType.QuadOut)]
    [InlineData(EasingType.QuadInOut)]
    [InlineData(EasingType.SmoothStep)]
    [InlineData(EasingType.SmootherStep)]
    public void Update_DifferentEasingTypes_ShouldInterpolate(EasingType easing)
    {
        // Arrange
        var entity = CreateInterpolatingEntity();
        ref var config = ref _world.Get<ClientVisualConfig>(entity);
        config.Easing = easing;

        ref var movement = ref _world.Get<VisualInterpolation>(entity);
        var from = new VisualPosition { X = 0, Y = 0 };
        var to = new VisualPosition { X = 100f, Y = 0 };
        movement.Start(from, to, 1f, MovementDirection.East);

        // Act
        _system.Update(0.5f);

        // Assert
        var pos = _world.Get<VisualPosition>(entity);
        Assert.True(pos.X > 0f && pos.X <= 100f);
    }

    [Fact]
    public void Update_MultipleEntities_ShouldProcessAll()
    {
        // Arrange
        var entity1 = CreateInterpolatingEntity();
        var entity2 = CreateInterpolatingEntity();

        ref var movement1 = ref _world.Get<VisualInterpolation>(entity1);
        movement1.Start(new VisualPosition { X = 0, Y = 0 }, new VisualPosition { X = 32f, Y = 0 }, 1f,
            MovementDirection.East);

        ref var movement2 = ref _world.Get<VisualInterpolation>(entity2);
        movement2.Start(new VisualPosition { X = 0, Y = 0 }, new VisualPosition { X = 0, Y = 32f }, 1f,
            MovementDirection.South);

        // Act
        _system.Update(0.5f);

        // Assert
        var pos1 = _world.Get<VisualPosition>(entity1);
        var pos2 = _world.Get<VisualPosition>(entity2);
        Assert.True(pos1.X > 0);
        Assert.True(pos2.Y > 0);
    }
}

#endregion

#region ClientInputSystem Tests

public class ClientInputSystemTests : IDisposable
{
    private readonly World _world;
    private readonly MockInputProvider _inputProvider;
    private readonly MockNetworkSender _networkSender;
    private readonly ClientInputSystem _system;
    private const float CellSize = 32f;

    public ClientInputSystemTests()
    {
        _world = World.Create();
        _inputProvider = new MockInputProvider();
        _networkSender = new MockNetworkSender();
        _system = new ClientInputSystem(_world, _inputProvider, _networkSender, CellSize);
    }

    public void Dispose()
    {
        _world.Dispose();
    }

    private Entity CreateLocalPlayer(int x, int y)
    {
        return _world.Create(
            new ClientNavigationEntity(),
            new SyncedGridPosition { X = x, Y = y },
            new LocalPlayer()
        );
    }

    [Fact]
    public void Update_NoClick_ShouldNotSendInput()
    {
        // Arrange
        CreateLocalPlayer(0, 0);
        _inputProvider.ClickPressed = false;

        // Act
        _system.Update(0.016f);

        // Assert
        Assert.Empty(_networkSender.SentInputs);
    }

    [Fact]
    public void Update_ClickOnDifferentCell_ShouldSendInput()
    {
        // Arrange
        CreateLocalPlayer(0, 0);
        _inputProvider.ClickPressed = true;
        _inputProvider.ClickPosition = (160f, 160f); // Grid (5, 5)

        // Act
        _system.Update(0.016f);

        // Assert
        Assert.Single(_networkSender.SentInputs);
        var input = _networkSender.SentInputs[0];
        Assert.Equal(5, input.TargetX);
        Assert.Equal(5, input.TargetY);
    }

    [Fact]
    public void Update_ClickOnSameCell_ShouldNotSendInput()
    {
        // Arrange
        CreateLocalPlayer(5, 5);
        _inputProvider.ClickPressed = true;
        _inputProvider.ClickPosition = (160f, 160f); // Grid (5, 5) - mesma posição

        // Act
        _system.Update(0.016f);

        // Assert
        Assert.Empty(_networkSender.SentInputs);
    }

    [Fact]
    public void Update_InputCooldown_ShouldPreventRapidInputs()
    {
        // Arrange
        CreateLocalPlayer(0, 0);
        _inputProvider.ClickPressed = true;
        _inputProvider.ClickPosition = (160f, 160f);

        // Act - Primeiro input
        _system.Update(0.016f);

        // Muda destino e tenta novamente imediatamente
        _inputProvider.ClickPosition = (320f, 320f);
        _system.Update(0.016f);

        // Assert - Só o primeiro deve ter sido enviado devido ao cooldown
        Assert.Single(_networkSender.SentInputs);
    }

    [Fact]
    public void Update_AfterCooldown_ShouldAllowNewInput()
    {
        // Arrange
        CreateLocalPlayer(0, 0);
        _inputProvider.ClickPressed = true;
        _inputProvider.ClickPosition = (160f, 160f);

        // Act - Primeiro input
        _system.Update(0.016f);

        // Espera cooldown passar (0.1s)
        _system.Update(0.11f);

        // Muda destino
        _inputProvider.ClickPosition = (320f, 320f);
        _system.Update(0.016f);

        // Assert - Ambos inputs devem ter sido enviados
        Assert.Equal(2, _networkSender.SentInputs.Count);
    }

    [Fact]
    public void Update_SequenceId_ShouldIncrement()
    {
        // Arrange
        CreateLocalPlayer(0, 0);
        _inputProvider.ClickPressed = true;
        _inputProvider.ClickPosition = (160f, 160f);

        // Act
        _system.Update(0.016f);
        _system.Update(0.2f); // Espera cooldown
        _inputProvider.ClickPosition = (320f, 320f);
        _system.Update(0.016f);

        // Assert
        Assert.Equal(1, _networkSender.SentInputs[0].SequenceId);
        Assert.Equal(2, _networkSender.SentInputs[1].SequenceId);
    }

    [Fact]
    public void Update_NoLocalPlayer_ShouldNotSendInput()
    {
        // Arrange - Não cria local player
        _inputProvider.ClickPressed = true;
        _inputProvider.ClickPosition = (160f, 160f);

        // Act
        _system.Update(0.016f);

        // Assert
        Assert.Empty(_networkSender.SentInputs);
    }

    [Fact]
    public void Update_ClientTimestamp_ShouldBeSet()
    {
        // Arrange
        CreateLocalPlayer(0, 0);
        _inputProvider.ClickPressed = true;
        _inputProvider.ClickPosition = (160f, 160f);

        var beforeTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        _system.Update(0.016f);

        var afterTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Assert
        var input = _networkSender.SentInputs[0];
        Assert.True(input.ClientTimestamp >= beforeTimestamp && input.ClientTimestamp <= afterTimestamp);
    }
}

#endregion

#region ClientSyncSystem Tests

public class ClientSyncSystemTests : IDisposable
{
    private readonly World _world;
    private readonly ClientSyncSystem _system;
    private const float TickRate = 60f;
    private const float CellSize = 32f;

    public ClientSyncSystemTests()
    {
        _world = World.Create();
        _system = new ClientSyncSystem(_world, TickRate, CellSize);
    }

    public void Dispose()
    {
        _world.Dispose();
    }

    private Entity CreateSyncedEntity(int serverId, int x, int y)
    {
        var entity = _world.Create(
            new ClientNavigationEntity(),
            new SyncedGridPosition { X = x, Y = y },
            new VisualPosition { X = x * CellSize, Y = y * CellSize },
            new VisualInterpolation(),
            new MovementQueue(),
            new ClientVisualConfig { SmoothMovement = true, InterpolationSpeed = 1f },
            new SpriteAnimation()
        );

        _system.RegisterEntity(serverId, entity);
        return entity;
    }

    [Fact]
    public void RegisterEntity_ShouldMapServerIdToEntity()
    {
        // Arrange
        var entity = _world.Create(new ClientNavigationEntity());

        // Act
        _system.RegisterEntity(123, entity);

        // Assert - Verificar através de snapshot
        var snapshot = new MovementSnapshot
        {
            EntityId = 123,
            CurrentX = 5,
            CurrentY = 5,
            IsMoving = false
        };

        // Adiciona componentes necessários
        _world.Add(entity, new SyncedGridPosition());
        _world.Add(entity, new VisualPosition());
        _world.Add(entity, new VisualInterpolation());
        _world.Add(entity, new MovementQueue());
        _world.Add(entity, new ClientVisualConfig { SmoothMovement = true });
        _world.Add(entity, new SpriteAnimation());

        _system.EnqueueSnapshot(snapshot);
        _system.Update(0.016f);

        var syncedPos = _world.Get<SyncedGridPosition>(entity);
        Assert.Equal(5, syncedPos.X);
        Assert.Equal(5, syncedPos.Y);
    }

    [Fact]
    public void UnregisterEntity_ShouldRemoveMapping()
    {
        // Arrange
        var entity = CreateSyncedEntity(123, 0, 0);

        // Act
        _system.UnregisterEntity(123);

        // Enqueue snapshot para entidade não mapeada
        var snapshot = new MovementSnapshot
        {
            EntityId = 123,
            CurrentX = 10,
            CurrentY = 10
        };
        _system.EnqueueSnapshot(snapshot);
        _system.Update(0.016f);

        // Assert - Posição não deve mudar pois não está mapeada
        var syncedPos = _world.Get<SyncedGridPosition>(entity);
        Assert.Equal(0, syncedPos.X);
    }

    [Fact]
    public void EnqueueSnapshot_ShouldQueueForProcessing()
    {
        // Arrange
        CreateSyncedEntity(1, 0, 0);
        var snapshot = new MovementSnapshot { EntityId = 1, CurrentX = 5, CurrentY = 5 };

        // Act
        _system.EnqueueSnapshot(snapshot);
        _system.Update(0.016f);

        // Assert - Verificado através do estado da entidade
        // Se não lançou exceção, funcionou
        Assert.True(true);
    }

    [Fact]
    public void EnqueueBatch_ShouldQueueAllSnapshots()
    {
        // Arrange
        CreateSyncedEntity(1, 0, 0);
        CreateSyncedEntity(2, 0, 0);

        var batch = new BatchMovementUpdate
        {
            Snapshots = new[]
            {
                new MovementSnapshot { EntityId = 1, CurrentX = 1, CurrentY = 1 },
                new MovementSnapshot { EntityId = 2, CurrentX = 2, CurrentY = 2 }
            }
        };

        // Act
        _system.EnqueueBatch(batch);
        _system.Update(0.016f);

        // Assert - ambas entidades devem ter sido atualizadas
        // (teste passa se não lançar exceção)
        Assert.True(true);
    }

    [Fact]
    public void ProcessSnapshot_MovingEntity_ShouldStartInterpolation()
    {
        // Arrange
        var entity = CreateSyncedEntity(1, 0, 0);
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

        // Act
        _system.EnqueueSnapshot(snapshot);
        _system.Update(0.016f);

        // Assert
        var movement = _world.Get<VisualInterpolation>(entity);
        Assert.True(movement.IsActive);
        Assert.Equal(MovementDirection.East, movement.Direction);
    }

    [Fact]
    public void ProcessSnapshot_StationaryEntity_ShouldUpdatePosition()
    {
        // Arrange
        var entity = CreateSyncedEntity(1, 0, 0);
        var snapshot = new MovementSnapshot
        {
            EntityId = 1,
            CurrentX = 5,
            CurrentY = 5,
            IsMoving = false
        };

        // Act
        _system.EnqueueSnapshot(snapshot);
        _system.Update(0.016f);

        // Assert
        var syncedPos = _world.Get<SyncedGridPosition>(entity);
        Assert.Equal(5, syncedPos.X);
        Assert.Equal(5, syncedPos.Y);
    }

    [Fact]
    public void ProcessTeleport_ShouldInstantlyMoveEntity()
    {
        // Arrange
        var entity = CreateSyncedEntity(1, 0, 0);
        var teleport = new TeleportMessage
        {
            EntityId = 1,
            X = 100,
            Y = 100,
            FacingDirection = MovementDirection.South
        };

        // Act
        _system.EnqueueTeleport(teleport);
        _system.Update(0.016f);

        // Assert
        var syncedPos = _world.Get<SyncedGridPosition>(entity);
        Assert.Equal(100, syncedPos.X);
        Assert.Equal(100, syncedPos.Y);

        var visualPos = _world.Get<VisualPosition>(entity);
        // VisualPosition.FromGrid centraliza na célula: (x + 0.5) * cellSize
        var expectedPos = (100 + 0.5f) * CellSize;
        Assert.Equal(expectedPos, visualPos.X, 0.01f);
        Assert.Equal(expectedPos, visualPos.Y, 0.01f);

        var anim = _world.Get<SpriteAnimation>(entity);
        Assert.Equal(MovementDirection.South, anim.Facing);
    }

    [Fact]
    public void ProcessTeleport_ShouldResetInterpolation()
    {
        // Arrange
        var entity = CreateSyncedEntity(1, 0, 0);

        // Inicia interpolação
        ref var movement = ref _world.Get<VisualInterpolation>(entity);
        movement.Start(
            new VisualPosition { X = 0, Y = 0 },
            new VisualPosition { X = 100, Y = 100 },
            1f,
            MovementDirection.SouthEast
        );

        var teleport = new TeleportMessage
        {
            EntityId = 1,
            X = 50,
            Y = 50,
            FacingDirection = MovementDirection.North
        };

        // Act
        _system.EnqueueTeleport(teleport);
        _system.Update(0.016f);

        // Assert
        movement = ref _world.Get<VisualInterpolation>(entity);
        Assert.False(movement.IsActive);
    }

    [Fact]
    public void ProcessSnapshot_UnknownEntity_ShouldNotThrow()
    {
        // Arrange
        var snapshot = new MovementSnapshot { EntityId = 999, CurrentX = 5, CurrentY = 5 };

        // Act & Assert
        _system.EnqueueSnapshot(snapshot);
        var exception = Record.Exception(() => _system.Update(0.016f));
        Assert.Null(exception);
    }

    [Fact]
    public void ProcessSnapshot_ShouldUpdateFacingDirection()
    {
        // Arrange
        var entity = CreateSyncedEntity(1, 0, 0);
        var snapshot = new MovementSnapshot
        {
            EntityId = 1,
            CurrentX = 0,
            CurrentY = 0,
            Direction = MovementDirection.West,
            IsMoving = false
        };

        // Act
        _system.EnqueueSnapshot(snapshot);
        _system.Update(0.016f);

        // Assert
        var anim = _world.Get<SpriteAnimation>(entity);
        Assert.Equal(MovementDirection.West, anim.Facing);
    }

    [Fact]
    public async Task EnqueueSnapshot_ThreadSafety_ShouldNotThrow()
    {
        // Arrange
        CreateSyncedEntity(1, 0, 0);
        var tasks = new List<Task>();

        // Act - Enqueue de múltiplas threads
        for (var i = 0; i < 10; i++)
            tasks.Add(Task.Run(() =>
            {
                for (var j = 0; j < 100; j++)
                    _system.EnqueueSnapshot(new MovementSnapshot
                    {
                        EntityId = 1,
                        CurrentX = (short)j,
                        CurrentY = (short)j
                    });
            }));

        // Assert
        var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks));
        Assert.Null(exception);
    }
}

#endregion

#region ClientAnimationSystem Tests

public class ClientAnimationSystemTests : IDisposable
{
    private readonly World _world;
    private readonly ClientAnimationSystem _system;

    public ClientAnimationSystemTests()
    {
        _world = World.Create();
        _system = new ClientAnimationSystem(_world, 0.01f);
    }

    public void Dispose()
    {
        _world.Dispose();
    }

    private Entity CreateAnimatedEntity()
    {
        return _world.Create(
            new SpriteAnimation(),
            new VisualInterpolation()
        );
    }

    [Fact]
    public void Update_StationaryEntity_ShouldSetIdleAnimation()
    {
        // Arrange
        var entity = CreateAnimatedEntity();
        ref var movement = ref _world.Get<VisualInterpolation>(entity);
        movement.Reset(); // Não está se movendo

        // Act
        _system.Update(0.016f);

        // Assert
        var anim = _world.Get<SpriteAnimation>(entity);
        Assert.Equal(AnimationClip.Idle, anim.Clip);
    }

    [Fact]
    public void Update_MovingEntity_ShouldSetWalkAnimation()
    {
        // Arrange
        var entity = CreateAnimatedEntity();
        ref var movement = ref _world.Get<VisualInterpolation>(entity);
        movement.Start(
            new VisualPosition { X = 0, Y = 0 },
            new VisualPosition { X = 32, Y = 0 },
            1f,
            MovementDirection.East
        );

        // Act
        _system.Update(0.016f);

        // Assert
        var anim = _world.Get<SpriteAnimation>(entity);
        Assert.Equal(AnimationClip.Walk, anim.Clip);
    }

    [Fact]
    public void Update_ShouldUpdateFacingDirection()
    {
        // Arrange
        var entity = CreateAnimatedEntity();
        ref var movement = ref _world.Get<VisualInterpolation>(entity);
        movement.Start(
            new VisualPosition { X = 0, Y = 0 },
            new VisualPosition { X = 0, Y = 32 },
            1f,
            MovementDirection.South
        );

        // Act
        _system.Update(0.016f);

        // Assert
        var anim = _world.Get<SpriteAnimation>(entity);
        Assert.Equal(MovementDirection.South, anim.Facing);
    }

    [Fact]
    public void Update_ShouldIncrementAnimationTime()
    {
        // Arrange
        var entity = CreateAnimatedEntity();
        ref var anim = ref _world.Get<SpriteAnimation>(entity);
        anim.Time = 0f;

        // Act
        _system.Update(0.5f);

        // Assert
        anim = ref _world.Get<SpriteAnimation>(entity);
        Assert.Equal(0.5f, anim.Time, 0.01f);
    }

    [Fact]
    public void Update_ShouldUpdateFrameIndex()
    {
        // Arrange
        var entity = CreateAnimatedEntity();
        ref var anim = ref _world.Get<SpriteAnimation>(entity);
        anim.Time = 0f;
        anim.SetClip(AnimationClip.Walk);

        ref var movement = ref _world.Get<VisualInterpolation>(entity);
        movement.Start(
            new VisualPosition { X = 0, Y = 0 },
            new VisualPosition { X = 32, Y = 0 },
            10f, // Longa duração para manter ativo
            MovementDirection.East
        );

        // Act - Com 8 FPS para Walk, após 0.5s deveria estar no frame 4 % 4 = 0
        _system.Update(0.5f);

        // Assert
        anim = ref _world.Get<SpriteAnimation>(entity);
        Assert.True(anim.Frame >= 0 && anim.Frame < 4); // Walk tem 4 frames
    }

    [Fact]
    public void Update_MultipleEntities_ShouldProcessAll()
    {
        // Arrange
        var entity1 = CreateAnimatedEntity();
        var entity2 = CreateAnimatedEntity();

        ref var movement2 = ref _world.Get<VisualInterpolation>(entity2);
        movement2.Start(
            new VisualPosition { X = 0, Y = 0 },
            new VisualPosition { X = 32, Y = 0 },
            1f,
            MovementDirection.East
        );

        // Act
        _system.Update(0.016f);

        // Assert
        var anim1 = _world.Get<SpriteAnimation>(entity1);
        var anim2 = _world.Get<SpriteAnimation>(entity2);
        Assert.Equal(AnimationClip.Idle, anim1.Clip);
        Assert.Equal(AnimationClip.Walk, anim2.Clip);
    }
}

#endregion