using Game.Domain.Commons;

namespace Game.Domain.Combat;

/// <summary>
/// Funções puras para cálculos de combate (inteiros com escala).
/// </summary>
public static class CombatCalculator
{
    public static int CeilDiv(int a, int b)
    {
        if (b <= 0) throw new ArgumentOutOfRangeException(nameof(b));
        return (a + b - 1) / b;
    }

    /// <summary>
    /// Converte AttackSpeed (permille) em cooldown (ticks).
    /// attackSpeed = 1000 => 1.000x => cooldown = base
    /// attackSpeed = 1500 => 1.500x => cooldown = base * 1000/1500
    /// </summary>
    public static int ComputeAttackCooldownTicks(int baseCooldownTicks, int attackSpeedPermille)
    {
        attackSpeedPermille = Math.Clamp(
            attackSpeedPermille,
            GameConstants.Combat.MIN_ATTACK_SPEED,
            GameConstants.Combat.MAX_ATTACK_SPEED);

        int ticks = CeilDiv(baseCooldownTicks * GameConstants.Combat.ATTACK_SPEED_SCALE, attackSpeedPermille);
        return Math.Max(GameConstants.Combat.MIN_ATTACK_COOLDOWN_TICKS, ticks);
    }

    public static int ClampAttackSpeed(int attackSpeedPermille) =>
        Math.Clamp(
            attackSpeedPermille,
            GameConstants.Combat.MIN_ATTACK_SPEED,
            GameConstants.Combat.MAX_ATTACK_SPEED);
}
