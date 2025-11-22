using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Logic;

public static partial class CombatLogic
{
    private const float MinAttacksPerSecond = 0.1f;
    private const float MaxAttacksPerSecond = 20f;
    private const int CriticalDamageMultiplier = 2;
    
    /// <summary>
    /// Calcula o dano total considerando ataque físico/mágico e defesa da vítima.
    /// </summary>
    public static int CalculateDamage(World world, in Entity attacker, in Entity targetEntity, AttackType attackType, bool isCritical)
    {
        if (!world.TryGet<AttackPower>(attacker, out var attack) || !world.TryGet<Defense>(targetEntity, out var defense))
            return 0;
        
        bool isMagical = attackType == AttackType.Magic;
        
        int attackPower = isMagical ? attack.Magical : attack.Physical;
        int defensePower = isMagical ? defense.Magical : defense.Physical;
        
        int multiplier = isCritical ? CriticalDamageMultiplier : 1;

        int baseDamage = Math.Max(1, attackPower - defensePower) * multiplier;
        float variance = 0.8f + (float)Random.Shared.NextDouble() * 0.4f;
        return (int)(baseDamage * variance);
    }
    
    /// <summary>
    /// Calcula o range de ataque com base no tipo de ataque.
    /// </summary>
    private static int GetAttackRange(AttackType type) => type switch
    {
        AttackType.Basic    => 1,
        AttackType.Heavy    => 1,
        AttackType.Critical => 1,
        AttackType.Magic    => 10,
        _ => 1
    };
    
    private static float GetAttackTypeSpeedMultiplier(AttackType type) => type switch
    {
        AttackType.Basic    => 1.00f,
        AttackType.Heavy    => 0.60f,
        AttackType.Critical => 0.80f,
        AttackType.Magic    => 0.90f,
        _ => 1.00f
    };
    
    private static DamageTimingPhase GetDamageTimingPhase(AttackType type) => type switch
    {
        AttackType.Basic    => DamageTimingPhase.Late,   // Dano no meio do golpe
        AttackType.Heavy    => DamageTimingPhase.Late,  // Dano no impacto final
        AttackType.Critical => DamageTimingPhase.Mid,   // Dano no momento crítico
        AttackType.Magic    => DamageTimingPhase.Early, // Dano quando lança o feitiço
        _ => DamageTimingPhase.Mid
    };
    
}