using Game.ECS.Schema.Components;

namespace Game.ECS.Logic;

public static class RegenLogic
{
    public static bool TryHeal(ref Health health, int amount)
    {
        if (amount <= 0)
            return false;
        
        int previous = health.Current;
        int newValue = Math.Min(health.Max, previous + amount);

        if (newValue == previous)
            return false;

        health.Current = newValue;
        return true;
    }

    public static bool TryRestoreMana(ref Mana mana, int amount)
    {
        if (amount <= 0)
            return false;

        int previous = mana.Current;
        int newValue = Math.Min(mana.Max, previous + amount);

        if (newValue == previous)
            return false;

        mana.Current = newValue;
        return true;
    }
    
}