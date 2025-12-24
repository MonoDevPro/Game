using System.Runtime.CompilerServices;

namespace Game.Domain.Attributes.Vitals.ValueObjects;

/// <summary>
/// Componente de mana da entidade.
/// </summary>
public struct Mana(int max, int regenPerTick = 1)
{
    public int Current = max;
    public int Maximum = max;
    public int RegenPerTick = regenPerTick;

    public readonly bool IsEmpty => Current <= 0;
    public readonly bool IsFull => Current >= Maximum;
    public readonly float Percentage => Maximum > 0 ? (float)Current / Maximum : 0f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryConsume(int amount)
    {
        if (Current >= amount)
        {
            Current -= amount;
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Regenerate()
    {
        Current = Math.Min(Maximum, Current + RegenPerTick);
    }

    public void Reset()
    {
        Current = Maximum;
    }
}