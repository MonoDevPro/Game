using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Extensions;

public static class DamageExtensions
{
    public static bool ApplyDamage(this World world, Entity victim, int damage, Entity? attacker = null)
    {
        if (!world.IsAlive(victim)) return false;
        if (!world.Has<Health>(victim)) return false;
        if (damage <= 0) return false;

        ref var health = ref world.Get<Health>(victim);
        int newHealth = Math.Max(0, health.Current - damage);
        if (newHealth == health.Current) return false;
        health.Current = newHealth;
        return true;
    }

    public static void ApplyDeferredDamage(this World world, Entity attacker, Entity victim, int amount, bool isCritical = false)
    {
        ref var damaged = ref world.AddOrGet<Damaged>(victim);
        damaged.Amount += amount;
        damaged.IsCritical = isCritical;
        damaged.SourceEntity = attacker;
        
        var dirty = world.TryGetRef<DirtyFlags>(victim, out var exists);
        if (exists) dirty.MarkDirty(DirtyComponentType.Damaged);
    }
    
    /// <summary>
    /// Calcula o dano total considerando ataque físico/mágico e defesa da vítima.
    /// </summary>
    public static int CalculateDamage(this in AttackPower attack, in Defense defense, bool isMagical = false)
    {
        int attackPower = isMagical ? attack.Magical : attack.Physical;
        int defensePower = isMagical ? defense.Magical : defense.Physical;

        int baseDamage = Math.Max(1, attackPower - defensePower);
        float variance = 0.8f + (float)Random.Shared.NextDouble() * 0.4f;
        return (int)(baseDamage * variance);
    }
}