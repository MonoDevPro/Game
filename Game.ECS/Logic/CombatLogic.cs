using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Logic;

public static class CombatLogic
{
        /// <summary>
    /// Calcula o dano total considerando ataque físico/mágico e defesa da vítima.
    /// </summary>
    public static int CalculateDamage(in AttackPower attack, in Defense defense, bool isMagical = false)
    {
        int attackPower = isMagical ? attack.Magical : attack.Physical;
        int defensePower = isMagical ? defense.Magical : defense.Physical;
        
        // Defesa reduz dano: max(1, ataque - defesa)
        int baseDamage = Math.Max(1, attackPower - defensePower);
        
        // Variação aleatória: ±20%
        float variance = 0.8f + (float)Random.Shared.NextDouble() * 0.4f;
        return (int)(baseDamage * variance);
    }

    /// <summary>
    /// Realiza um ataque corpo-a-corpo com validações de cooldown e alcance.
    /// </summary>
    public static bool TryAttack(World world, Entity attacker, Entity target)
    {
        if (!world.IsAlive(attacker) || !world.IsAlive(target))
            return false;

        if (!world.TryGet(attacker, out Position attackerPos) ||
            !world.TryGet(target, out Position targetPos) ||
            !world.TryGet(attacker, out AttackPower attackPower) ||
            !world.TryGet(target, out Defense defense) ||
            !world.TryGet(attacker, out CombatState combat) ||
            !world.TryGet(attacker, out Attackable attackable))
            return false;

        if (combat.LastAttackTime > 0f)
            return false;

        int distance = attackerPos.ManhattanDistance(targetPos);
        if (distance > SimulationConfig.MaxMeleeAttackRange)
            return false;

        if (world.Has<Dead>(target) || world.Has<Invulnerable>(target))
            return false;

        float baseSpeed = Math.Max(0.1f, attackable.BaseSpeed);
        float modifier = Math.Max(0.1f, attackable.CurrentModifier);
        float attacksPerSecond = baseSpeed * modifier;
        combat.LastAttackTime = 1f / attacksPerSecond;
        combat.InCombat = true;
        world.Set(attacker, combat);

        int damage = CalculateDamage(attackPower, defense);
        if (ApplyDamageInternal(world, target, damage, attacker))
            return true;
        return false;
    }

    /// <summary>
    /// Aplica dano a uma entidade alvo.
    /// </summary>
    public static bool TryDamage(World world, Entity target, int damage, Entity? attacker = null)
    {
        if (!world.IsAlive(target) || !world.Has<Health>(target))
            return false;

        if (damage <= 0)
            return false;

        return ApplyDamageInternal(world, target, damage, attacker);
    }

    /// <summary>
    /// Restaura vida de uma entidade (para poções, curas, etc).
    /// </summary>
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

    /// <summary>
    /// Restaura mana de uma entidade.
    /// </summary>
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
    
    private static bool ApplyDamageInternal(World world, Entity target, int damage, Entity? attacker)
    {
        ref Health health = ref world.Get<Health>(target);
        int previous = health.Current;
        int newValue = Math.Max(0, previous - damage);

        if (newValue == previous)
            return false;

        health.Current = newValue;
        world.Set(target, health);

        if (health.Current <= 0 && !world.Has<Dead>(target))
            world.Add<Dead>(target);

        return true;
    }
}