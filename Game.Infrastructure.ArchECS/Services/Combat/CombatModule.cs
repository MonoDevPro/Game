using Arch.Core;
using Arch.System;
using Game.Contracts;
using Game.Infrastructure.ArchECS.Services.Combat.Components;
using Game.Infrastructure.ArchECS.Services.Combat.Systems;
using Game.Infrastructure.ArchECS.Services.Map;

namespace Game.Infrastructure.ArchECS.Services.Combat;

/// <summary>
/// Módulo de combate do servidor (isolado para plug-in gradual).
/// </summary>
public sealed class CombatModule : IDisposable
{
    public CombatConfig Config { get; }
    public CombatEntityRegistry Registry { get; } = new();

    private readonly World _world;
    private readonly WorldMap _worldMap;
    private readonly CombatEventBuffer _events = new();
    private readonly CombatVitalsBuffer _vitals = new();
    private readonly Group<long> _systems;
    private bool _disposed;

    public CombatModule(World world, WorldMap worldMap, CombatConfig? config = null)
    {
        _world = world;
        _worldMap = worldMap;
        Config = config ?? CombatConfig.Default;

        _systems = new Group<long>(
            "ServerCombat",
            new CombatAttackSystem(world, _worldMap, Config, _events, _vitals, Registry),
            new CombatProjectileSystem(world, _worldMap, _events, _vitals, Registry)
        );

        _systems.Initialize();
    }

    public void Tick(long serverTick)
    {
        _systems.BeforeUpdate(in serverTick);
        _systems.Update(in serverTick);
        _systems.AfterUpdate(in serverTick);
    }

    public void RegisterEntity(int entityId, Entity entity)
    {
        Registry.Register(entityId, entity);
        EnsureCombatComponents(entity);
    }
    
    public void UnregisterEntity(int entityId)
    {
        Registry.Unregister(entityId);
    }

    public bool TryGetEntity(int entityId, out Entity entity)
        => Registry.TryGetEntity(entityId, out entity);

    public bool TryGetCombatStats(int entityId, out CombatStats stats)
    {
        if (!Registry.TryGetEntity(entityId, out var entity))
        {
            stats = default;
            return false;
        }

        if (!_world.Has<CombatStats>(entity))
        {
            stats = default;
            return false;
        }

        stats = _world.Get<CombatStats>(entity);
        return true;
    }

    public bool RequestBasicAttack(int entityId, int dirX, int dirY, long serverTick)
    {
        if (dirX == 0 && dirY == 0)
            return false;

        if (!Registry.TryGetEntity(entityId, out var entity))
            return false;

        dirX = Math.Clamp(dirX, -1, 1);
        dirY = Math.Clamp(dirY, -1, 1);

        if (_world.Has<AttackRequest>(entity))
            _world.Remove<AttackRequest>(entity);

        _world.Add(entity, new AttackRequest
        {
            DirX = dirX,
            DirY = dirY,
            RequestedTick = serverTick
        });

        return true;
    }

    public void EnsureCombatComponents(Entity entity)
    {
        if (!_world.Has<CombatStats>(entity))
            _world.Add(entity, new CombatStats());
        if (!_world.Has<AttackCooldown>(entity))
            _world.Add(entity, new AttackCooldown());
        if (!_world.Has<TeamId>(entity))
            _world.Add(entity, new TeamId());
        if (!_world.Has<VocationTag>(entity))
            _world.Add(entity, new VocationTag());
    }

    public void ApplyCombatState(Entity entity, in CombatStats stats, byte vocation, int teamId)
    {
        EnsureCombatComponents(entity);
        _world.Get<CombatStats>(entity) = stats;
        _world.Get<VocationTag>(entity) = new VocationTag { Value = vocation };
        _world.Get<TeamId>(entity) = new TeamId { Value = teamId };
    }

    public bool TryDrainEvents(out List<CombatEvent> events)
        => _events.TryDrain(out events);
    
    public bool TryDrainVitals(out List<CombatVitalUpdate> updates)
        => _vitals.TryDrain(out updates);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Registry.Clear();
        _events.Clear();
        _vitals.Clear();
        _systems.Dispose();
    }
}
