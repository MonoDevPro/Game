using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Extensions;
using Game.ECS.Services;

namespace Game.ECS.Logic;

public static partial class CombatLogic
{
    public static bool CheckAttackCooldown(in CombatState combat) => combat.LastAttackTime <= 0f;

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
}