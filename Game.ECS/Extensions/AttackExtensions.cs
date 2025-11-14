using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Extensions;

public static class AttackExtensions
{
    /// <summary>
    /// Realiza um ataque corpo-a-corpo com validações de cooldown e alcance.
    /// Calcula o dano mas NÃO o aplica imediatamente - será aplicado pelo DeferredDamageSystem.
    /// </summary>
    public static bool TryAttack(this World world, Entity attacker, Entity target, AttackType attackType, out int damage)
    {
        damage = 0;

        if (!world.IsAlive(attacker) || !world.IsAlive(target))
            return false;

        if (!world.TryGet(attacker, out Position attackerPos) ||
            !world.TryGet(target, out Position targetPos) ||
            !world.TryGet(attacker, out AttackPower attackPower) ||
            !world.TryGet(target, out Defense defense))
            return false;

        int distance = attackerPos.ManhattanDistance(targetPos);
        if (distance > SimulationConfig.MaxMeleeAttackRange)
            return false;

        if (world.Has<Dead>(target) || world.Has<Invulnerable>(target))
            return false;

        // Calcula dano mas não aplica - será aplicado pelo DeferredDamageSystem
        damage = attackPower.CalculateDamage(defense);
        return true;
    }
}