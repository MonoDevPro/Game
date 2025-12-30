using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Domain.Commons.ValueObjects.Attributes;
using Game.Domain.Commons.ValueObjects.Character;

namespace Game.Domain.Commons.ValueObjects.Vitals;

/// <summary>
/// Componente de mana da entidade.
/// Component ECS para gerenciar pontos de mana (MP).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Mana(int current, int max, int regenPerTick)
{
    public const int MpPerIntelligence = 5;
    public const int MpPerLevel = 3;

    public const int MinRegenPerTick = 1;
    public const int RegenDivisor = 10;

    public int Current = current;
    public int Maximum = max;
    public int RegenPerTick = regenPerTick;

    public readonly bool IsEmpty => Current <= 0;
    public readonly bool IsFull => Current >= Maximum;

    /// <summary>Percentual em 0..100 (inteiro).</summary>
    public readonly int Percentage => Maximum > 0 ? (Current * 100) / Maximum : 0;

    public static Mana Create(BaseStats total, Progress progress, int current = 0)
    {
        int maxMp = (MpPerIntelligence * total.Intelligence) + (progress.Level * MpPerLevel);
        int regen = Math.Max(MinRegenPerTick, total.Spirit / RegenDivisor);

        // default: cheio (coerente com HP)
        int currentMp = current > 0 ? Math.Clamp(current, 0, maxMp) : maxMp;
        return new Mana(current: currentMp, max: maxMp, regenPerTick: regen);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryConsume(int amount)
    {
        if (amount <= 0) return true;
        if (Current < amount) return false;
        Current -= amount;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Regenerate() => Current = Math.Min(Maximum, Current + RegenPerTick);

    public void Reset() => Current = Maximum;
}