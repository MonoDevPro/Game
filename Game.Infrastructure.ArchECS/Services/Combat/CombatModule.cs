using Arch.Core;
using Arch.System;
using Game.Contracts;
using Game.Infrastructure.ArchECS.Commons;
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
    private readonly Events.CombatEventBuffer _events;
    private readonly Group<long> _systems;
    private bool _disposed;

    private readonly CentralEntityRegistry _registry;

    public CombatModule(World world, WorldMap worldMap, CombatConfig? config = null)
    {
        _events = new Events.CombatEventBuffer(world);
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
        int Level,
        long Experience,
        int Strength,
        int Endurance,
        int Agility,
        int Intelligence,
        int Willpower,
        int MaxHealth,
        int MaxMana,
        int CurrentHealth,
        int CurrentMana,
        byte vocation,
        int teamId)
    {
        if (!_world.Has<AttackCooldown>(entity))
            _world.Add(entity, new AttackCooldown());

        var stats = new CombatStats
        {
            Level = Level,
            Experience = Experience,
            Strength = Strength,
            Endurance = Endurance,
            Agility = Agility,
            Intelligence = Intelligence,
            Willpower = Willpower,
            MaxHealth = MaxHealth,
            MaxMana = MaxMana,
            CurrentHealth = CurrentHealth,
            CurrentMana = CurrentMana
        };

        ApplyCombatStats(entity, stats);
        ApplyVocation(entity, vocation);
        ApplyTeamId(entity, teamId);
    }

    private void ApplyCombatStats(
        Entity entity,
        CombatStats stats)
    {
        _world.AddOrReplace<CombatStats>(entity, stats);
    }

    private void ApplyVocation(Entity entity, byte vocation)
    {
        _world.AddOrReplace<VocationTag>(entity, new VocationTag { Value = vocation });
    }

    private void ApplyTeamId(Entity entity, int teamId)
    {
        _world.AddOrReplace<TeamId>(entity, new TeamId { Value = teamId });
    }

    public void RemoveCombatComponents(Entity entity)
    {
        _world.RemoveIfExists<CombatStats>(entity);
        _world.RemoveIfExists<VocationTag>(entity);
        _world.RemoveIfExists<TeamId>(entity);
        _world.RemoveIfExists<AttackCooldown>(entity);
        _world.RemoveIfExists<AttackRequest>(entity);
    }

    public bool RequestBasicAttack(Entity entity, int dirX, int dirY, long serverTick)
    {
        if (dirX == 0 && dirY == 0)
            return false;

        dirX = Math.Clamp(dirX, -1, 1);
        dirY = Math.Clamp(dirY, -1, 1);

        _world.AddOrReplace<AttackRequest>(entity, new AttackRequest
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
