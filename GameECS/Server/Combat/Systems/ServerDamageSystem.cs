using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameECS.Shared.Combat.Components;

namespace GameECS.Server.Combat.Systems;

/// <summary>
/// Sistema que processa dano pendente e verifica mortes.
/// </summary>
public sealed partial class ServerDamageSystem : BaseSystem<World, long>
{
    private readonly Action<int, int, long>? _onEntityDeath;

    public ServerDamageSystem(
        World world,
        Action<int, int, long>? onEntityDeath = null) : base(world)
    {
        _onEntityDeath = onEntityDeath;
    }

    [Query]
    [All<Health, DamageBuffer>, None<Dead>]
    private void ProcessDamageBufferQuery(
        [Data] in long serverTick,
        Entity entity,
        ref Health health,
        ref DamageBuffer buffer)
    {
        if (buffer.Count == 0) return;

        ProcessDamageBuffer(entity, ref health, ref buffer, serverTick);
    }

    private unsafe void ProcessDamageBuffer(Entity entity, ref Health health, ref DamageBuffer buffer, long serverTick)
    {
        int lastAttackerId = -1;

        for (int i = 0; i < buffer.Count; i++)
        {
            int damage = buffer.DamageValues[i];
            int attackerId = buffer.AttackerIds[i];

            health.TakeDamage(damage);
            lastAttackerId = attackerId;

            if (health.IsDead)
                break;
        }

        buffer.Clear();

        if (health.IsDead && !World.Has<Dead>(entity))
        {
            World.Add<Dead>(entity);
            
            // Remove capacidade de atacar
            if (World.Has<CanAttack>(entity))
                World.Remove<CanAttack>(entity);

            _onEntityDeath?.Invoke(entity.Id, lastAttackerId, serverTick);
        }
    }
}

/// <summary>
/// Sistema que regenera mana das entidades.
/// </summary>
public sealed partial class ServerManaRegenSystem : BaseSystem<World, long>
{
    private readonly int _regenIntervalTicks;
    private long _lastRegenTick;

    public ServerManaRegenSystem(World world, int regenIntervalTicks = 60) : base(world)
    {
        _regenIntervalTicks = regenIntervalTicks;
    }

    [Query]
    [All<Mana>, None<Dead>]
    private void ProcessManaRegen(
        [Data] in long serverTick,
        ref Mana mana)
    {
        if (serverTick - _lastRegenTick < _regenIntervalTicks)
            return;

        _lastRegenTick = serverTick;
        mana.Regenerate();
    }
}

/// <summary>
/// Sistema que processa saída de combate após período de inatividade.
/// </summary>
public sealed partial class ServerCombatStateSystem : BaseSystem<World, long>
{
    private readonly int _combatTimeoutTicks;

    public ServerCombatStateSystem(World world, int combatTimeoutTicks = 300) : base(world)
    {
        _combatTimeoutTicks = combatTimeoutTicks;
    }

    [Query]
    [All<InCombat>, None<Dead>]
    private void ProcessCombatTimeout(
        [Data] in long serverTick,
        Entity entity,
        ref InCombat combat)
    {
        if (serverTick - combat.LastCombatTick >= _combatTimeoutTicks)
        {
            World.Remove<InCombat>(entity);
        }
    }
}
