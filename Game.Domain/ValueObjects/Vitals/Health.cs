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
    public double TakeDamage(double damage)
    {
        double actualDamage = Math.Min(Current, Math.Max(0, damage));
        Current -= actualDamage;
        return actualDamage;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Heal(double amount)
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
    
    public Health WithMax(double newMax)
    {
        return new Health(newMax, RegenPerTick)
        {
            Current = Math.Min(Current, newMax)
        };
    }
    
    public Health WithRegen(double newRegen)
    {
        return new Health(Maximum, newRegen)
        {
            Current = Current
        };
    }
    
    public Health WithPercentage(double newPercentage)
    {
        var clampedPercentage = Math.Clamp(newPercentage, 0.0, 1.0);
        return new Health(Maximum, RegenPerTick)
        {
            Current = Maximum * clampedPercentage
        };
    }
    
    public Health WithCurrent(double newCurrent)
    {
        return new Health(Maximum, RegenPerTick)
        {
            Current = Math.Clamp(newCurrent, 0.0, Maximum)
        };
    }
    
    
}