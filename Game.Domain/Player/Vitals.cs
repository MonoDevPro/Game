using System;

namespace Game.Domain.Player;

/// <summary>
/// Recursos vitais do personagem (HP/MP).
/// </summary>
public readonly record struct Vitals(int Hp, int Mp, int MaxHp, int MaxMp)
{
    public bool IsAlive => Hp > 0;
    public bool HasMana => Mp > 0;
    public double HpPercent => MaxHp > 0 ? (double)Hp / MaxHp : 0;
    public double MpPercent => MaxMp > 0 ? (double)Mp / MaxMp : 0;

    public Vitals WithDamage(int damage) => this with { Hp = Math.Max(0, Hp - damage) };
    public Vitals WithHeal(int amount) => this with { Hp = Math.Min(MaxHp, Hp + amount) };
    public Vitals WithManaSpent(int amount) => this with { Mp = Math.Max(0, Mp - amount) };
    public Vitals WithManaRestored(int amount) => this with { Mp = Math.Min(MaxMp, Mp + amount) };
    public Vitals AtFullCapacity() => this with { Hp = MaxHp, Mp = MaxMp };
}

/// <summary>
/// Taxa de regeneração de recursos vitais por tick.
/// </summary>
public readonly record struct VitalRecovery(int HpRegenPerTick, int MpRegenPerTick)
{
    public static VitalRecovery None => default;
}