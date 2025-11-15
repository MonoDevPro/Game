using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Extensions;

public static class DamageExtensions
{
    public static void ApplyDeferredDamage(this World world, Entity attacker, Entity victim, int amount, bool isCritical = false)
    {
        if (!world.IsAlive(victim))
            return;
        ref var damaged = ref world.AddOrGet<Damaged>(victim);
        damaged.Amount += amount;
        damaged.IsCritical = isCritical;
        damaged.SourceEntity = attacker;
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