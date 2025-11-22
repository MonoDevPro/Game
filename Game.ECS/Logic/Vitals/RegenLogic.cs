using Game.ECS.Components;

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
    
    /// <summary>
    /// Lida com regeneração baseada em taxa float + acumulação, aplicando
    /// apenas os pontos inteiros quando acumulados.
    /// Exemplos de uso:
    /// - HP: RegenRate = 0.7/s → acumula até >= 1, aplica 1, sobra fração.
    /// - MP: mesmo padrão.
    /// </summary>
    /// <param name="current">valor atual (HP/MP/etc).</param>
    /// <param name="max">valor máximo.</param>
    /// <param name="regenRatePerSecond">taxa de regen por segundo (float).</param>
    /// <param name="deltaTime">delta de tempo em segundos.</param>
    /// <param name="accumulated">
    /// acumulador de frações; será incrementado e terá os inteiros descontados.
    /// </param>
    /// <returns>true se o valor atual foi incrementado.</returns>
    public static bool ApplyRegeneration(
        ref int current,
        int max,
        float regenRatePerSecond,
        float deltaTime,
        ref float accumulated)
    {
        // Já está cheio, zera acumulação para não vlogar float ao infinito.
        if (current >= max)
        {
            accumulated = 0f;
            return false;
        }

        if (regenRatePerSecond <= 0f || deltaTime <= 0f)
            return false;

        accumulated += regenRatePerSecond * deltaTime;

        if (accumulated < 1.0f)
            return false;

        int regenToApply = (int)accumulated;
        if (regenToApply <= 0)
            return false;

        int previous = current;
        current = Math.Min(max, current + regenToApply);

        // Desconta apenas o que virou inteiro.
        accumulated -= regenToApply;

        return current != previous;
    }
    
}