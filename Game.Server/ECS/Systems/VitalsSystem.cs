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

        // ✅ Marca como em combate e zera o timer desde o último hit
        combat.InCombat = true;
        combat.TimeSinceLastHit = 0f;

        World.Remove<Damaged>(victim);
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

        dirty.MarkDirty(DirtyComponentType.Vitals);
        World.Add<Dead>(entity);
    }
    
    
    [Query]
    [All<Health, DirtyFlags, CombatState>] // ✅ inclui CombatState
    [None<Dead>]
    private void ProcessHealthRegeneration(
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
}
