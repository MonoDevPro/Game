using Arch.Core;
using Arch.System;
using Game.ECS.Navigation.Components;
using Game.ECS.Navigation.Data;
using Game.ECS.Navigation.Systems;

namespace Game.ECS. Navigation;

/// <summary>
/// Módulo de navegação para CLIENTE.
/// Gerencia interpolação visual e sincronização com servidor.
/// </summary>
public sealed class ClientNavigationModule : IDisposable
{
    public float CellSize { get; }
    public float TickRate { get; }

    private readonly World _world;
    private readonly Group<float> _systems;
    private readonly NetworkSyncSystem _syncSystem;
    private readonly Dictionary<int, Entity> _serverEntityMap;
    private bool _disposed;

    public ClientNavigationModule(
        World world,
        float cellSize = 32f,
        float tickRate = 60f,
        IInputProvider?  inputProvider = null,
        INetworkSender? networkSender = null)
    {
        _world = world;
        CellSize = cellSize;
        TickRate = tickRate;
        _serverEntityMap = new Dictionary<int, Entity>(256);

        _syncSystem = new NetworkSyncSystem(world, tickRate, cellSize);

        var systemsList = new List<BaseSystem<World, float>>
        {
            _syncSystem,
            new ClientMovementInterpolationSystem(world, cellSize),
            new ClientAnimationSystem(world)
        };

        // Adiciona sistema de input se providers foram fornecidos
        if (inputProvider != null && networkSender != null)
        {
            systemsList.Add(new ClientInputSystem(world, inputProvider, networkSender, cellSize));
        }

        _systems = new Group<float>("ClientNavigation", systemsList. ToArray());
        _systems.Initialize();
    }

    /// <summary>
    /// Atualiza sistemas do cliente.
    /// </summary>
    public void Update(float deltaTime)
    {
        _systems.BeforeUpdate(in deltaTime);
        _systems.Update(in deltaTime);
        _systems.AfterUpdate(in deltaTime);
    }

    /// <summary>
    /// Cria entidade visual para um agente do servidor.
    /// </summary>
    public Entity CreateEntity(
        int serverId,
        int gridX,
        int gridY,
        bool isLocalPlayer = false,
        ClientVisualSettings?  settings = null)
    {
        var actualSettings = settings ?? ClientVisualSettings.Default;
        var visualPos = VisualPosition.FromGrid(gridX, gridY, CellSize);

        var entity = _world. Create(
            new SyncedGridPosition(gridX, gridY),
            visualPos,
            new ClientMovementState { ToPosition = visualPos },
            new MovementBuffer(),
            actualSettings,
            new AnimationState
            {
                FacingDirection = MovementDirection.South,
                CurrentAnimation = AnimationType. Idle
            },
            new ClientNavigationEntity()
        );

        if (isLocalPlayer)
        {
            _world.Add<LocalPlayer>(entity);
        }

        // Registra mapeamento
        _serverEntityMap[serverId] = entity;
        _syncSystem.RegisterEntity(serverId, entity);

        return entity;
    }

    /// <summary>
    /// Remove entidade. 
    /// </summary>
    public void DestroyEntity(int serverId)
    {
        if (_serverEntityMap.TryGetValue(serverId, out var entity))
        {
            _syncSystem.UnregisterEntity(serverId);
            _serverEntityMap.Remove(serverId);

            if (_world.IsAlive(entity))
            {
                _world. Destroy(entity);
            }
        }
    }

    /// <summary>
    /// Processa snapshot recebido do servidor. 
    /// </summary>
    public void OnMovementSnapshot(MovementSnapshot snapshot)
    {
        _syncSystem.EnqueueSnapshot(snapshot);
    }

    /// <summary>
    /// Processa batch de snapshots.
    /// </summary>
    public void OnBatchUpdate(BatchMovementUpdate batch)
    {
        _syncSystem.EnqueueBatch(batch);
    }

    /// <summary>
    /// Processa teleporte. 
    /// </summary>
    public void OnTeleport(TeleportMessage teleport)
    {
        _syncSystem.EnqueueTeleport(teleport);
    }

    /// <summary>
    /// Obtém posição visual de uma entidade (para renderização).
    /// </summary>
    public VisualPosition GetVisualPosition(int serverId)
    {
        if (_serverEntityMap.TryGetValue(serverId, out var entity) && _world.IsAlive(entity))
        {
            return _world.Get<VisualPosition>(entity);
        }
        return default;
    }

    /// <summary>
    /// Obtém estado de animação. 
    /// </summary>
    public AnimationState GetAnimationState(int serverId)
    {
        if (_serverEntityMap.TryGetValue(serverId, out var entity) && _world.IsAlive(entity))
        {
            return _world.Get<AnimationState>(entity);
        }
        return default;
    }

    /// <summary>
    /// Obtém entidade local pelo ID do servidor.
    /// </summary>
    public Entity?  GetEntity(int serverId)
    {
        return _serverEntityMap.TryGetValue(serverId, out var entity) ? entity : null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _systems.Dispose();
    }
}