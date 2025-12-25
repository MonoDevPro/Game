using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Game.Domain.ValueObjects.Vitals;

/// <summary>
/// Componente de vida da entidade.
/// Component ECS para gerenciar pontos de vida (HP).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Health(double max, double regenPerTick = 1)
{
    public double Current = max;
    public double Maximum = max;
    public double RegenPerTick = regenPerTick;

    public readonly bool IsDead => Current <= 0;
    public readonly bool IsFullHealth => Current >= Maximum;
    public readonly double Percentage => Maximum > 0 ? Current / Maximum : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double TakeDamage(int damage)
    {
        double actualDamage = Math.Min(Current, Math.Max(0, damage));
        Current -= actualDamage;
        return actualDamage;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Heal(int amount)
    {
        double actualHeal = Math.Min(Maximum - Current, Math.Max(0, amount));
        Current += actualHeal;
        return actualHeal;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Regenerate()
    {
        Current = Math.Min(Maximum, Current + RegenPerTick);
    }

    public void SetMax(double newMax)
    {
        Maximum = Math.Max(1, newMax);
        Current = Math.Min(Current, Maximum);
    }

    public void Reset()
    {
        Current = Maximum;
    }
}