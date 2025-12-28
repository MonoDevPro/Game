using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Domain.ValueObjects.Attributes;
using Game.Domain.ValueObjects.Character;
using Game.Domain.ValueObjects.Combat;

namespace Game.Domain.ValueObjects.Vitals;

/// <summary>
/// Componente de vida da entidade.
/// Component ECS para gerenciar pontos de vida (HP).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Health(double current, double max, double regenPerTick)
{
    public const int HpPerConstitution = 10;
    public const int HpPerLevel = 5;
    public const int MinRegenPerTick = 1;
    public const int RegenDivisor = 10;
    
    public double Current = current;
    public double Maximum = max;
    public double RegenPerTick = regenPerTick;

    public readonly bool IsDead => Current <= 0;
    public readonly bool IsFullHealth => Current >= Maximum;
    public readonly double Percentage => Maximum > 0 ? Current / Maximum : 0;
    
    public static Health Create(BaseStats total, Progress progress, double current = 0)
    {
        var maxHp = HpPerConstitution * total.Constitution + progress.Level * HpPerLevel;
        var hpRegenPerTick = Math.Max(MinRegenPerTick, total.Constitution / RegenDivisor);
        var currentHp = current > 0 ? Math.Clamp(current, 0, maxHp) : maxHp;
        return new Health(current: currentHp, max: maxHp, regenPerTick: hpRegenPerTick);
    }
    
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
    public void Regenerate() => Current = Math.Min(Maximum, Current + RegenPerTick);

    public void SetMax(double newMax)
    {
        Maximum = Math.Max(1, newMax);
        Current = Math.Min(Current, Maximum);
    }

    public void Reset() => Current = Maximum;
    
    public Health WithCurrent(double newCurrent) => new Health(Math.Clamp(newCurrent, 0.0, Maximum), Maximum, RegenPerTick);
    public Health WithMax(double newMax) => new Health(Math.Min(Current, newMax), newMax, RegenPerTick);
    public Health WithRegen(double newRegen) => new Health(Current, Maximum, newRegen);
    public Health WithPercentage(double newPercentage) => new Health(Maximum * Math.Clamp(newPercentage, 0.0, 1.0), Maximum, RegenPerTick);
    
    
}