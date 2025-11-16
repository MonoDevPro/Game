using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por regeneração de vida e mana.
/// Processa entidades que têm Health e Mana, aplicando regeneração por tick.
/// </summary>
public sealed partial class VitalsSystem(World world, ILogger<VitalsSystem>? logger = null)
    : GameSystem(world)
{
    [Query]
    [All<Damaged, Health, CombatState>] // ✅ inclui CombatState
    [None<Dead, Invulnerable>]
    private void ProcessDamage(
        in Entity victim,
        in Damaged damaged,
        ref Health health,
        ref CombatState combat,   // ✅ novo
        ref DirtyFlags dirty)
    {
        var damage = damaged.Amount;
        if (damage <= 0)
        {
            World.Remove<Damaged>(victim);
            return;
        }

        int newHealth = Math.Max(0, health.Current - damage);
        if (newHealth != health.Current)
            dirty.MarkDirty(DirtyComponentType.Vitals);

        health.Current = newHealth;
        if (health.Current <= 0)
            World.Add<Dead>(victim);

        // ✅ Marca como em combate e zera o timer desde o último hit
        combat.InCombat = true;
        combat.TimeSinceLastHit = 0f;

        World.Remove<Damaged>(victim);

        logger?.LogDebug(
            "Dano imediato aplicado: {Damage} para {Target} de {Attacker}",
            damaged.Amount,
            World.TryGet(victim, out NetworkId targetNetId) ? targetNetId.Value : -1,
            World.TryGet(damaged.SourceEntity, out NetworkId attackerNetId) ? attackerNetId.Value : -1);
    }
    
    [Query]
    [All<Health, DirtyFlags, CombatState>] // ✅ inclui CombatState
    [None<Dead>]
    private void ProcessHealthRegeneration(
        in Entity e,
        ref Health health,
        ref DirtyFlags dirty,
        ref CombatState combat,        // ✅ novo
        [Data] float deltaTime)
    {
        // ✅ Se ainda está em combate, não regenera
        if (combat.InCombat)
            return;

        if (health.Current >= health.Max)
        {
            health.AccumulatedRegeneration = 0f;
            return;
        }

        health.AccumulatedRegeneration += health.RegenerationRate * deltaTime;

        if (!(health.AccumulatedRegeneration >= 1.0f))
            return;

        int regenToApply = (int)health.AccumulatedRegeneration;
        int newValue = Math.Min(health.Max, health.Current + regenToApply);

        if (newValue == health.Current)
            return;

        health.Current = newValue;
        health.AccumulatedRegeneration -= regenToApply;
        dirty.MarkDirty(DirtyComponentType.Vitals);
    }

    [Query]
    [All<Mana, DirtyFlags, CombatState>] // ✅ inclui CombatState
    [None<Dead>]
    private void ProcessManaRegeneration(
        in Entity e,
        ref Mana mana,
        ref DirtyFlags dirty,
        ref CombatState combat,        // ✅ novo
        [Data] float deltaTime)
    {
        if (combat.InCombat)
            return;

        if (mana.Current >= mana.Max)
        {
            mana.AccumulatedRegeneration = 0f;
            return;
        }

        mana.AccumulatedRegeneration += mana.RegenerationRate * deltaTime;

        if (mana.AccumulatedRegeneration < 1.0f)
            return;

        int regenToApply = (int)mana.AccumulatedRegeneration;
        int newValue = Math.Min(mana.Max, mana.Current + regenToApply);

        if (newValue != mana.Current)
        {
            mana.Current = newValue;
            mana.AccumulatedRegeneration -= regenToApply;
            dirty.MarkDirty(DirtyComponentType.Vitals);
        }
    }

    [Query]
    [All<Health>]
    [None<Dead>]
    private void ProcessDeath(
        in Entity entity,
        ref Health health,
        ref DirtyFlags dirty)
    {
        if (health.Current > 0)
            return;

        World.Add<Dead>(entity);
        dirty.MarkDirty(DirtyComponentType.Vitals);
    }
}
