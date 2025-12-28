using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Domain.ValueObjects.Attributes;
using Game.Domain.ValueObjects.Character;

namespace Game.Domain.ValueObjects.Vitals;

/// <summary>
/// Componente de mana da entidade.
/// Component ECS para gerenciar pontos de mana (MP).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Mana(double current, double max, double regenPerTick = 1)
{
    public const int MpPerIntelligence = 5;
    public const int MpPerLevel = 3;
    public const int MinRegenPerTick = 1;
    public const int RegenDivisor = 10;
    
    public double Current = current;
    public double Maximum = max;
    public double RegenPerTick = regenPerTick;

    public readonly bool IsEmpty => Current <= 0;
    public readonly bool IsFull => Current >= Maximum;
    public readonly double Percentage => Maximum > 0 ? (float)Current / Maximum : 0;
    
    public static Mana Create(BaseStats total, Progress progress, double current = 0)
    {
        var currentMp = current > 0 ? Math.Clamp(current, 0, total.Intelligence * MpPerIntelligence + progress.Level * MpPerLevel) : 0;
        var maxMp = MpPerIntelligence * total.Intelligence + progress.Level * MpPerLevel;
        var mpRegenPerTick= Math.Max(MinRegenPerTick, total.Spirit / RegenDivisor);
        return new Mana(current: currentMp, max: maxMp, regenPerTick: mpRegenPerTick);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryConsume(double amount)
    {
        if (Current >= amount)
        {
            Current -= amount;
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Regenerate() => Current = Math.Min(Maximum, Current + RegenPerTick);
    public void Reset() => Current = Maximum;
    
    public Mana WithCurrent(double current) => new Mana(Maximum, RegenPerTick) { Current = current };
    
}