using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Extensions;
using Game.ECS.Services;

namespace Game.ECS.Logic;

public static partial class CombatLogic
{
    // Limites de sanidade para taxa de ataque (ataques por segundo)
    private const float MinAttacksPerSecond = 0.1f;
    private const float MaxAttacksPerSecond = 20f;

    public static float GetAttackTypeSpeedMultiplier(AttackType type) => type switch
    {
        AttackType.Basic    => 1.00f,
        AttackType.Heavy    => 0.60f,
        AttackType.Critical => 0.80f,
        AttackType.Magic    => 0.90f,
        _ => 1.00f
    };

    /// <summary>
    /// Define em qual fase da animação o dano deve ser aplicado para cada tipo de ataque.
    /// </summary>
    public static DamageTimingPhase GetDamageTimingPhase(AttackType type) => type switch
    {
        AttackType.Basic    => DamageTimingPhase.Mid,   // Dano no meio do golpe
        AttackType.Heavy    => DamageTimingPhase.Late,  // Dano no impacto final
        AttackType.Critical => DamageTimingPhase.Mid,   // Dano no momento crítico
        AttackType.Magic    => DamageTimingPhase.Early, // Dano quando lança o feitiço
        _ => DamageTimingPhase.Mid
    };

    /// <summary>
    /// Verifica se o dano deve ser aplicado baseado no progresso da animação.
    /// Retorna true apenas uma vez quando a fase apropriada é atingida.
    /// </summary>
    public static bool ShouldApplyDamage(in Attack action)
    {
        if (action.DamageApplied || action.TotalDuration <= 0f)
            return false;

        float progress = 1f - (action.RemainingDuration / action.TotalDuration);
        var timingPhase = GetDamageTimingPhase(action.Type);

        return timingPhase switch
        {
            DamageTimingPhase.Early => progress >= 0.33f,  // 33% da animação
            DamageTimingPhase.Mid   => progress >= 0.66f,  // 66% da animação
            DamageTimingPhase.Late  => progress >= 0.90f,  // 90% da animação
            _ => false
        };
    }

}

public static partial class CombatLogic
{
    public static bool CheckAttackCooldown(in CombatState combat) => combat.LastAttackTime <= 0f;

    public static bool TryDamage(World world, Entity target, int damage)
    {
        if (!world.IsAlive(target) || !world.Has<Health>(target))
            return false;

        if (damage <= 0)
            return false;

        return ApplyDamageInternal(world, target, damage);
    }

    public static bool TryHeal(World world, Entity target, int amount, Entity? healer = null)
    {
        if (!world.IsAlive(target))
            return false;

        if (!world.TryGet(target, out Health health))
            return false;

        int previous = health.Current;
        int newValue = Math.Min(health.Max, previous + amount);

        if (newValue == previous)
            return false;

        health.Current = newValue;
        world.Set(target, health);
        return true;
    }

    public static bool TryRestoreMana(World world, Entity target, int amount, Entity? source = null)
    {
        if (!world.IsAlive(target))
            return false;

        if (!world.TryGet(target, out Mana mana))
            return false;

        int previous = mana.Current;
        int newValue = Math.Min(mana.Max, previous + amount);

        if (newValue == previous)
            return false;

        mana.Current = newValue;
        world.Set(target, mana);
        return true;
    }
    
    private static bool ApplyDamageInternal(World world, Entity victim, int damage)
    {
        if (!world.TryGet(victim, out Health health))
            return false;
        
        int previous = health.Current;
        int newValue = Math.Max(0, previous - damage);

        if (newValue == previous)
            return false;

        health.Current = newValue;
        world.Set(victim, health);
        world.Remove<Damaged>(victim);

        if (health.Current <= 0 && !world.Has<Dead>(victim))
            world.Add<Dead>(victim);

        return true;
    }
}