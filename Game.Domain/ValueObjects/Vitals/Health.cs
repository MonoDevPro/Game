using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Game.Domain.ValueObjects.Vitals;

/// <summary>
/// Componente de vida da entidade.
/// Component ECS para gerenciar pontos de vida (HP).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Health(int max, int regenPerTick = 1)
{
    public int Current = max;
    public int Maximum = max;
    public int RegenPerTick = regenPerTick;

    public readonly bool IsDead => Current <= 0;
    public readonly bool IsFullHealth => Current >= Maximum;
    public readonly float Percentage => Maximum > 0 ? (float)Current / Maximum : 0f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int TakeDamage(int damage)
    {
        int actualDamage = Math.Min(Current, Math.Max(0, damage));
        Current -= actualDamage;
        return actualDamage;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Heal(int amount)
    {
        int actualHeal = Math.Min(Maximum - Current, Math.Max(0, amount));
        Current += actualHeal;
        return actualHeal;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Regenerate()
    {
        Current = Math.Min(Maximum, Current + RegenPerTick);
    }

    public void SetMax(int newMax)
    {
        Maximum = Math.Max(1, newMax);
        Current = Math.Min(Current, Maximum);
    }

    public void Reset()
    {
        Current = Maximum;
    }
}