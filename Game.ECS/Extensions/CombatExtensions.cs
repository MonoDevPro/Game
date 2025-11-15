using Game.ECS.Components;
using Game.ECS.Logic;

namespace Game.ECS.Extensions;

public static class CombatExtensions
{
    // Limites de sanidade para taxa de ataque (ataques por segundo)
    private const float MinAttacksPerSecond = 0.1f;
    private const float MaxAttacksPerSecond = 20f;
    
    public static void ReduceCooldown(this ref CombatState combat, float deltaTime)
    {
        if (combat.LastAttackTime <= 0f) return;
        combat.LastAttackTime = MathF.Max(0f, combat.LastAttackTime - deltaTime);
    }
    
    /// <summary>
    /// Verifica se o dano deve ser aplicado baseado no progresso da animação.
    /// Retorna true apenas uma vez quando a fase apropriada é atingida.
    /// </summary>
    public static bool ShouldApplyDamage(this in Attack action)
    {
        if (action.DamageApplied || action.TotalDuration <= 0f)
            return false;

        float progress = 1f - (action.RemainingDuration / action.TotalDuration);
        var timingPhase = action.Type.GetDamageTimingPhase();

        return timingPhase switch
        {
            DamageTimingPhase.Early => progress >= 0.33f,  // 33% da animação
            DamageTimingPhase.Mid   => progress >= 0.66f,  // 66% da animação
            DamageTimingPhase.Late  => progress >= 0.90f,  // 90% da animação
            _ => false
        };
    }
    
    public static DamageTimingPhase GetDamageTimingPhase(this AttackType type) => type switch
    {
        AttackType.Basic    => DamageTimingPhase.Mid,   // Dano no meio do golpe
        AttackType.Heavy    => DamageTimingPhase.Late,  // Dano no impacto final
        AttackType.Critical => DamageTimingPhase.Mid,   // Dano no momento crítico
        AttackType.Magic    => DamageTimingPhase.Early, // Dano quando lança o feitiço
        _ => DamageTimingPhase.Mid
    };
    
    public static float CalculateAttackCooldownSeconds(this in Attackable attackable, AttackType type = AttackType.Basic, float externalMultiplier = 1f)
    {
        float baseSpeed = MathF.Max(0.05f, attackable.BaseSpeed);
        float modifier  = MathF.Max(0.05f, attackable.CurrentModifier);
        float typeMul   = MathF.Max(0.05f, type.GetAttackTypeSpeedMultiplier());
        float extraMul  = MathF.Max(0.05f, externalMultiplier);

        float aps = baseSpeed * modifier * typeMul * extraMul;
        if (aps < MinAttacksPerSecond) aps = MinAttacksPerSecond;
        else if (aps > MaxAttacksPerSecond) aps = MaxAttacksPerSecond;

        return 1f / aps;
    }
    
    private static float GetAttackTypeSpeedMultiplier(this AttackType type) => type switch
    {
        AttackType.Basic    => 1.00f,
        AttackType.Heavy    => 0.60f,
        AttackType.Critical => 0.80f,
        AttackType.Magic    => 0.90f,
        _ => 1.00f
    };
    
}