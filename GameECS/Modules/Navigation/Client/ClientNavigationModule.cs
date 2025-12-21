using Arch.Core;
using Arch.System;
using GameECS.Modules.Navigation.Client.Components;
using GameECS.Modules.Navigation.Client.Systems;
using GameECS.Modules.Navigation.Shared.Components;
using GameECS.Modules.Navigation.Shared.Data;

namespace GameECS.Modules.Navigation.Client;

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
    private readonly ClientSyncSystem _syncSystem;
    private readonly Dictionary<int, Entity> _serverEntityMap;
    private bool _disposed;

    public ClientNavigationModule(
        World world,
        float cellSize = 32f,
        float tickRate = 60f,
        IInputProvider? inputProvider = null,
        INetworkSender? networkSender = null)
    {
        _world = world;
        CellSize = cellSize;
        TickRate = tickRate;
        _serverEntityMap = new Dictionary<int, Entity>(256);

        _syncSystem = new ClientSyncSystem(world, tickRate, cellSize);

        var systemsList = new List<BaseSystem<World, float>>
        {
            _syncSystem,
            new ClientInterpolationSystem(world, cellSize),
            new ClientAnimationSystem(world)
        };

        // Adiciona sistema de input se providers foram fornecidos
        if (inputProvider != null && networkSender != null)
        {
            systemsList.Add(new ClientInputSystem(world, inputProvider, networkSender, cellSize));
        }

        _systems = new Group<float>("ClientNavigation", systemsList.ToArray());
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
        ClientVisualConfig? settings = null)
    {
        var actualSettings = settings ?? ClientVisualConfig.Default;
        var visualPos = VisualPosition.FromGrid(gridX, gridY, CellSize);

        var entity = _world.Create(
            new SyncedGridPosition(gridX, gridY),
            visualPos,
            new VisualInterpolation() { To = visualPos },
            new MovementQueue(),
            actualSettings,
            new SpriteAnimation()
            {
                Facing = MovementDirection.South,
                Clip = AnimationClip.Idle
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
                _world.Destroy(entity);
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
    public SpriteAnimation GetAnimationState(int serverId)
    {
        if (_serverEntityMap.TryGetValue(serverId, out var entity) && _world.IsAlive(entity))
        {
            return _world.Get<SpriteAnimation>(entity);
        }
        return default;
    }

    /// <summary>
    /// Obtém entidade local pelo ID do servidor.
    /// </summary>
    public Entity? GetEntity(int serverId)
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