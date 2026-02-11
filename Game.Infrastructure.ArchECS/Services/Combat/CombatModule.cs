using Arch.Core;
using Arch.System;
using Game.Contracts;
using Game.Infrastructure.ArchECS.Services.Combat.Components;
using Game.Infrastructure.ArchECS.Services.Combat.Systems;
using Game.Infrastructure.ArchECS.Services.EntityRegistry;
using Game.Infrastructure.ArchECS.Services.Navigation.Map;

namespace Game.Infrastructure.ArchECS.Services.Combat;

/// <summary>
/// Módulo de combate do servidor (isolado para plug-in gradual).
/// </summary>
public sealed class CombatModule : IDisposable
{
    public CombatConfig Config { get; }

    private readonly World _world;
    private readonly CombatEventBuffer _events;
    private readonly Group<long> _systems;
    private bool _disposed;
    
    private readonly CentralEntityRegistry _registry;

    public CombatModule(World world, WorldMap worldMap, CombatConfig? config = null)
    {
        _events = new CombatEventBuffer(world);
        _world = world;
        Config = config ?? CombatConfig.Default;
        
        _registry = world.GetEntityRegistry();

        _systems = new Group<long>(
            "ServerCombat",
            new CombatAttackSystem(world, worldMap, Config),
            new CombatProjectileSystem(world, worldMap),
            _events
        );

        _systems.Initialize();
    }

    public void Tick(long serverTick)
    {
        _systems.BeforeUpdate(in serverTick);
        _systems.Update(in serverTick);
        _systems.AfterUpdate(in serverTick);
    }

    public void AddCombatComponents(
        Entity entity, 
        in CombatStats stats, 
        byte vocation, 
        int teamId)
    {
        if (!_world.Has<AttackCooldown>(entity))
            _world.Add(entity, new AttackCooldown());
        
        ApplyCombatStats(entity, in stats);
        ApplyVocation(entity, vocation);
        ApplyTeamId(entity, teamId);
    }
    
    public void ApplyCombatStats(Entity entity, in CombatStats stats)
    {
        ref var combatStats = ref _world.AddOrGet<CombatStats>(entity);
        combatStats = stats;
    }
    
    public void ApplyVocation(Entity entity, byte vocation)
    {
        ref var vocationTag = ref _world.AddOrGet<VocationTag>(entity);
        vocationTag.Value = vocation;
    }
    
    public void ApplyTeamId(Entity entity, int teamId)
    {
        ref var team = ref _world.AddOrGet<TeamId>(entity);
        team.Value = teamId;
    }
    
    public void RemoveCombatComponents(Entity entity)
    {
        if (_world.Has<CombatStats>(entity))
            _world.Remove<CombatStats>(entity);
        if (_world.Has<VocationTag>(entity))
            _world.Remove<VocationTag>(entity);
        if (_world.Has<TeamId>(entity))
            _world.Remove<TeamId>(entity);
        if (_world.Has<AttackCooldown>(entity))
            _world.Remove<AttackCooldown>(entity);
        if (_world.Has<AttackRequest>(entity))
            _world.Remove<AttackRequest>(entity);
    }

    public bool RequestBasicAttack(Entity entity, int dirX, int dirY, long serverTick)
    {
        if (dirX == 0 && dirY == 0)
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

    public bool TryDrainEvents(out List<CombatEvent> events)
        => _events.TryDrain(out events);
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _registry.Clear();
        _systems.Dispose();
    }
}
