using Arch.Core;
using Arch.System;
using Game.Domain.ValueObjects.Attributes;
using Game.Domain.ValueObjects.Vitals;
using Game.Domain.ValueObjects.Vocation;
using Game.Domain.ValueObjects.Combat;
using Game.Domain.Enums;
using GameECS.Server.Combat.Components;
using GameECS.Server.Combat.Systems;
using GameECS.Shared.Combat.Components;
using GameECS.Shared.Combat.Core;
using GameECS.Shared.Combat.Data;
using GameECS.Shared.Combat.Systems;
using GameECS.Shared.Entities.Components;
using GameECS.Shared.Entities.Data;
using CombatConfig = GameECS.Server.Combat.Components.CombatConfig;
using DamageType = GameECS.Shared.Combat.Data.DamageType;

namespace GameECS.Server.Combat;

/// <summary>
/// Módulo de combate do servidor.
/// Gerencia ataques, dano e estado de combate.
/// </summary>
public sealed class ServerCombatModule : IDisposable
{
    public Shared.Combat.Data.CombatConfig Config { get; }
    public CombatService CombatService { get; }
    public CombatLog CombatLog { get; }

    private readonly World _world;
    private readonly Group<long> _systems;
    private bool _disposed;

    /// <summary>
    /// Evento disparado quando dano é aplicado.
    /// </summary>
    public event Action<DamageMessage>? OnDamageDealt;

    /// <summary>
    /// Evento disparado quando uma entidade morre.
    /// </summary>
    public event Action<DeathMessage>? OnEntityDeath;

    public ServerCombatModule(World world, Shared.Combat.Data.CombatConfig? config = null)
    {
        _world = world;
        Config = config ?? Shared.Combat.Data.CombatConfig.Default;
        CombatLog = new CombatLog();
        CombatService = new CombatService(Config, CombatLog);

        _systems = new Group<long>("ServerCombat",
            new ServerAttackRequestSystem(
                world, 
                CombatService, 
                Config.MaxAttackRequestsPerTick,
                OnDamageDealtInternal,
                OnEntityDeathInternal),
            new ServerDamageSystem(world, OnDeathInternal),
            new ServerManaRegenSystem(world),
            new ServerCombatStateSystem(world));

        _systems.Initialize();
    }

    /// <summary>
    /// Processa tick do servidor.
    /// </summary>
    public void Tick(long serverTick)
    {
        _systems.BeforeUpdate(in serverTick);
        _systems.Update(in serverTick);
        _systems.AfterUpdate(in serverTick);
    }

    /// <summary>
    /// Cria uma entidade de combate com vocação específica.
    /// </summary>
    public Entity CreateCombatant(VocationType vocation, int level = 1)
    {
        var vocationStats = VocationStats.GetForVocation(vocation);
        var stats = Stats.FromVocation(vocation);
        var serverConfig = GetServerConfigForVocation(vocation);

        // Aplica scaling de level
        int healthBonus = (level - 1) * 10;
        int manaBonus = (level - 1) * 5;

        var entity = _world.Create(
            new Health(vocationStats.BaseHealth + healthBonus),
            new Mana(vocationStats.BaseMana + manaBonus),
            stats,
            new Vocation { } (vocation, level),
            new AttackCooldown(),
            serverConfig,
            new CombatStatistics(),
            new CombatEntity(),
            new CanAttack()
        );

        return entity;
    }

    /// <summary>
    /// Adiciona componentes de combate a uma entidade existente.
    /// </summary>
    public void AddCombatComponents(Entity entity, VocationType vocation, int level = 1)
    {
        var vocationStats = Stats.GetForVocation(vocation);
        var combatStats = CombatStats.FromVocation(vocation);
        var serverConfig = GetServerConfigForVocation(vocation);

        int healthBonus = (level - 1) * 10;
        int manaBonus = (level - 1) * 5;

        _world.Add(entity,
            new Health(vocationStats.BaseHealth + healthBonus),
            new Mana(vocationStats.BaseMana + manaBonus),
            combatStats,
            new PlayerVocation(vocation, level),
            new AttackCooldown(),
            serverConfig,
            new CombatStatistics(),
            new CombatEntity(),
            new CanAttack()
        );
    }

    /// <summary>
    /// Solicita um ataque básico contra um alvo.
    /// </summary>
    public void RequestAttack(Entity attacker, int targetEntityId, long currentTick)
    {
        if (_world.Has<Dead>(attacker)) return;
        if (!_world.Has<CanAttack>(attacker)) return;

        if (_world.Has<AttackRequest>(attacker))
            _world.Remove<AttackRequest>(attacker);

        _world.Add(attacker, AttackRequest.Create(targetEntityId, currentTick));
    }

    /// <summary>
    /// Solicita um ataque básico contra uma entidade alvo.
    /// </summary>
    public void RequestAttack(Entity attacker, Entity target, long currentTick)
    {
        RequestAttack(attacker, target.Id, currentTick);
    }

    /// <summary>
    /// Aplica dano direto a uma entidade (bypass de defesa).
    /// </summary>
    public void ApplyDirectDamage(Entity target, int damage, int attackerId, DamageType type = DamageType.True)
    {
        if (_world.Has<Dead>(target)) return;
        if (_world.Has<Invulnerable>(target)) return;

        ref var health = ref _world.Get<Health>(target);
        health.TakeDamage(damage);

        if (health.IsDead && !_world.Has<Dead>(target))
        {
            _world.Add<Dead>(target);
            if (_world.Has<CanAttack>(target))
                _world.Remove<CanAttack>(target);
        }
    }

    /// <summary>
    /// Cura uma entidade.
    /// </summary>
    public int HealEntity(Entity entity, int amount)
    {
        if (_world.Has<Dead>(entity)) return 0;
        if (!_world.Has<Health>(entity)) return 0;

        ref var health = ref _world.Get<Health>(entity);
        return health.Heal(amount);
    }

    /// <summary>
    /// Ressuscita uma entidade morta.
    /// </summary>
    public void Resurrect(Entity entity, float healthPercentage = 0.5f)
    {
        if (!_world.Has<Dead>(entity)) return;

        _world.Remove<Dead>(entity);

        if (_world.Has<Health>(entity))
        {
            ref var health = ref _world.Get<Health>(entity);
            health.Current = Math.Max(1, (int)(health.Maximum * healthPercentage));
        }

        if (!_world.Has<CanAttack>(entity))
            _world.Add<CanAttack>(entity);
    }

    /// <summary>
    /// Define invulnerabilidade de uma entidade.
    /// </summary>
    public void SetInvulnerable(Entity entity, bool invulnerable)
    {
        if (invulnerable && !_world.Has<Invulnerable>(entity))
            _world.Add<Invulnerable>(entity);
        else if (!invulnerable && _world.Has<Invulnerable>(entity))
            _world.Remove<Invulnerable>(entity);
    }

    /// <summary>
    /// Obtém estatísticas de combate de uma entidade.
    /// </summary>
    public CombatStatistics? GetStatistics(Entity entity)
    {
        if (_world.Has<CombatStatistics>(entity))
            return _world.Get<CombatStatistics>(entity);
        return null;
    }

    /// <summary>
    /// Verifica se entidade está viva.
    /// </summary>
    public bool IsAlive(Entity entity)
    {
        return !_world.Has<Dead>(entity);
    }

    /// <summary>
    /// Verifica se entidade está em combate.
    /// </summary>
    public bool IsInCombat(Entity entity)
    {
        return _world.Has<InCombat>(entity);
    }

    private CombatConfig GetServerConfigForVocation(VocationType vocation)
    {
        return vocation switch
        {
            VocationType.Knight => Shared.Combat.Data.CombatConfig.Knight,
            VocationType.Mage => Shared.Combat.Data.CombatConfig.Mage,
            VocationType.Archer => Shared.Combat.Data.CombatConfig.Archer,
            _ => Shared.Combat.Data.CombatConfig.Default
        };
    }

    private void OnDamageDealtInternal(DamageMessage msg) => OnDamageDealt?.Invoke(msg);
    private void OnEntityDeathInternal(DeathMessage msg) => OnEntityDeath?.Invoke(msg);
    private void OnDeathInternal(int entityId, int killerId, long tick) 
        => OnEntityDeath?.Invoke(new DeathMessage { EntityId = entityId, KillerId = killerId, ServerTick = tick });

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _systems.Dispose();
    }
}
