using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Domain.Commons.ValueObjects.Attributes;
using Game.Domain.Commons.ValueObjects.Character;

namespace Game.Domain.Commons.ValueObjects.Vitals;

/// <summary>
/// Componente de vida da entidade.
/// Component ECS para gerenciar pontos de vida (HP).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Health(int current, int max, int regenPerTick)
{
    public const int HpPerConstitution = 10;
    public const int HpPerLevel = 5;

    public const int MinRegenPerTick = 1;
    public const int RegenDivisor = 10;

    public int Current = current;
    public int Maximum = max;
    public int RegenPerTick = regenPerTick;

    public readonly bool IsDead => Current <= 0;
    public readonly bool IsFullHealth => Current >= Maximum;

    /// <summary>Percentual em 0..100 (inteiro).</summary>
    public readonly int Percentage => Maximum > 0 ? (Current * 100) / Maximum : 0;

    public static Health Create(BaseStats total, Progress progress, int current = 0)
    {
        int maxHp = (HpPerConstitution * total.Constitution) + (progress.Level * HpPerLevel);
        int regen = Math.Max(MinRegenPerTick, total.Constitution / RegenDivisor);

        int currentHp = current > 0 ? Math.Clamp(current, 0, maxHp) : maxHp;
        return new Health(current: currentHp, max: maxHp, regenPerTick: regen);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int TakeDamage(int damage)
    {
        int actual = Math.Min(Current, Math.Max(0, damage));
        Current -= actual;
        return actual;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Heal(int amount)
    {
        int actual = Math.Min(Maximum - Current, Math.Max(0, amount));
        Current += actual;
        return actual;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Regenerate() => Current = Math.Min(Maximum, Current + RegenPerTick);

    public void SetMax(int newMax)
    {
        Maximum = Math.Max(1, newMax);
        Current = Math.Min(Current, Maximum);
    }

    public void Reset() => Current = Maximum;
}