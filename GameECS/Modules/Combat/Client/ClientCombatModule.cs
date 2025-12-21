using Arch.Core;
using Arch.System;
using GameECS.Modules.Combat.Client.Components;
using GameECS.Modules.Combat.Client.Systems;
using GameECS.Modules.Combat.Shared.Data;

namespace GameECS.Modules.Combat.Client;

/// <summary>
/// Módulo de combate para CLIENTE.
/// Gerencia visualização, animações e sincronização com servidor.
/// </summary>
public sealed class ClientCombatModule : IDisposable, ICombatNetworkReceiver
{
    private readonly World _world;
    private readonly Group<float> _systems;
    private readonly ClientCombatSyncSystem _syncSystem;
    private readonly Dictionary<int, Entity> _serverEntityMap;
    private bool _disposed;

    public ClientCombatModule(
        World world,
        ICombatInputProvider? inputProvider = null,
        ICombatNetworkSender? networkSender = null)
    {
        _world = world;
        _serverEntityMap = new Dictionary<int, Entity>(256);

        _syncSystem = new ClientCombatSyncSystem(world);

        var systemsList = new List<BaseSystem<World, float>>
        {
            _syncSystem,
            new ClientHealthBarSystem(world),
            new ClientFloatingDamageSystem(world),
            new ClientAttackAnimationSystem(world)
        };

        // Adiciona sistema de input se providers foram fornecidos
        if (inputProvider != null && networkSender != null)
        {
            systemsList.Add(new ClientCombatInputSystem(world, inputProvider, networkSender));
        }

        _systems = new Group<float>("ClientCombat", systemsList.ToArray());
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
    /// Cria entidade visual de combate para um agente do servidor.
    /// </summary>
    public Entity CreateEntity(
        int serverId,
        VocationType vocation,
        int health,
        int maxHealth,
        int mana,
        int maxMana,
        bool isLocalPlayer = false)
    {
        var entity = _world.Create(
            new SyncedHealth { Current = health, Maximum = maxHealth },
            new SyncedMana { Current = mana, Maximum = maxMana },
            HealthBar.Default,
            ManaBar.Default,
            new AttackAnimation(),
            new FloatingDamageBuffer(),
            new ClientCombatEntity()
        );

        if (isLocalPlayer)
        {
            _world.Add<LocalCombatPlayer>(entity);
        }

        // Registra mapeamento
        _serverEntityMap[serverId] = entity;
        _syncSystem.RegisterEntity(serverId, entity);

        return entity;
    }

    /// <summary>
    /// Adiciona componentes de combate a uma entidade existente.
    /// </summary>
    public void AddCombatComponents(
        Entity entity,
        int serverId,
        int health,
        int maxHealth,
        int mana,
        int maxMana,
        bool isLocalPlayer = false)
    {
        _world.Add(entity,
            new SyncedHealth { Current = health, Maximum = maxHealth },
            new SyncedMana { Current = mana, Maximum = maxMana },
            HealthBar.Default,
            ManaBar.Default,
            new AttackAnimation(),
            new FloatingDamageBuffer(),
            new ClientCombatEntity()
        );

        if (isLocalPlayer)
        {
            _world.Add<LocalCombatPlayer>(entity);
        }

        _serverEntityMap[serverId] = entity;
        _syncSystem.RegisterEntity(serverId, entity);
    }

    /// <summary>
    /// Remove entidade do módulo.
    /// </summary>
    public void RemoveEntity(int serverId)
    {
        if (_serverEntityMap.TryGetValue(serverId, out var entity))
        {
            _serverEntityMap.Remove(serverId);
            _syncSystem.UnregisterEntity(serverId);

            if (_world.IsAlive(entity))
            {
                _world.Destroy(entity);
            }
        }
    }

    /// <summary>
    /// Obtém entidade do cliente pelo ID do servidor.
    /// </summary>
    public bool TryGetEntity(int serverId, out Entity entity)
    {
        return _serverEntityMap.TryGetValue(serverId, out entity);
    }

    /// <summary>
    /// Inicia animação de ataque em uma entidade.
    /// </summary>
    public void StartAttackAnimation(int serverId, int targetId, VocationType vocation, float duration)
    {
        if (!TryGetEntity(serverId, out var entity)) return;

        if (_world.Has<AttackAnimation>(entity))
        {
            ref var animation = ref _world.Get<AttackAnimation>(entity);
            animation.Start(targetId, vocation, duration);
        }
    }

    /// <summary>
    /// Adiciona texto flutuante de dano.
    /// </summary>
    public void AddFloatingDamage(int targetServerId, int damage, bool isCritical, 
        DamageType type, float x, float y)
    {
        if (!TryGetEntity(targetServerId, out var entity)) return;

        if (_world.Has<FloatingDamageBuffer>(entity))
        {
            ref var buffer = ref _world.Get<FloatingDamageBuffer>(entity);
            buffer.Add(FloatingDamageText.Create(damage, x, y, isCritical, type));
        }
    }

    #region ICombatNetworkReceiver Implementation

    public void OnDamageReceived(DamageNetworkMessage message)
    {
        // Atualiza vida do alvo
        if (TryGetEntity(message.TargetId, out var targetEntity))
        {
            // O dano já foi aplicado no servidor, apenas mostramos visualmente
            AddFloatingDamage(message.TargetId, message.Damage, message.IsCritical, 
                message.Type, 0, 0);  // Posição será ajustada pelo sistema de renderização
        }

        // Inicia animação de ataque no atacante
        if (TryGetEntity(message.AttackerId, out var attackerEntity))
        {
            // Duração baseada no tipo seria ideal
            StartAttackAnimation(message.AttackerId, message.TargetId, VocationType.None, 0.5f);
        }
    }

    public void OnDeathReceived(DeathNetworkMessage message)
    {
        if (TryGetEntity(message.EntityId, out var entity))
        {
            if (!_world.Has<VisuallyDead>(entity))
                _world.Add<VisuallyDead>(entity);
        }
    }

    public void OnHealthUpdated(int entityId, int current, int max, long serverTick)
    {
        _syncSystem.ProcessHealthUpdate(entityId, current, max, serverTick);
    }

    public void OnManaUpdated(int entityId, int current, int max, long serverTick)
    {
        _syncSystem.ProcessManaUpdate(entityId, current, max, serverTick);
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _systems.Dispose();
    }
}
