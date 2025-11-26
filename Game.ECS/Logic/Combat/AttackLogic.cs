using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Logic;

public static partial class CombatLogic
{
    public static bool CheckAttackCooldown(in CombatState combat) 
        => combat.AttackCooldownTimer <= 0f;
    
    public static void ReduceCooldown(ref CombatState combat, float deltaTime) 
        => combat.AttackCooldownTimer = MathF.Max(0f, combat.AttackCooldownTimer - deltaTime);
    
    public static bool CheckAttackDistance(in Position attackerPos, in Position targetPos, AttackType attackType)
    {
        int distance = attackerPos.EuclideanDistanceSquared(targetPos);
        return distance <= GetAttackRange(attackType);
    }
    
    /// <summary>
    /// Verifica se o dano deve ser aplicado baseado no progresso da animação.
    /// Retorna true apenas uma vez quando a fase apropriada é atingida.
    /// </summary>
    public static bool ShouldApplyDamage(in Attack action)
    {
        if (action.DamageApplied || action.TotalDuration <= 0f)
            return false;

        float progress = 1f - (action.RemainingDuration / action.TotalDuration);
        var timingPhase = GetDamageTimingPhase(action.Type);

        return timingPhase switch
        {
            DamageTimingPhase.Early => progress >= 0.33f,  // 33% da animação
            DamageTimingPhase.Mid   => progress >= 0.66f,  // 66% da animação
            DamageTimingPhase.Late  => progress >= 0.90f,  // 90% da animação
            _ => false
        };
    }
    
    public static void EnterCombat(World world, in Entity entity)
    {
        if (!world.TryGet<CombatState>(entity, out var combat)) 
            return;
        
        combat.InCombat = true;
        combat.TimeSinceLastHit = 0f;
        world.Set(entity, combat);
    }
    
    public static float CalculateAttackCooldownSeconds(in Attackable attackable, AttackType type = AttackType.Basic, float externalMultiplier = 1f)
    {
        float baseSpeed = MathF.Max(0.05f, attackable.BaseSpeed);
        float modifier  = MathF.Max(0.05f, attackable.CurrentModifier);
        float typeMul   = MathF.Max(0.05f, GetAttackTypeSpeedMultiplier(type));
        float extraMul  = MathF.Max(0.05f, externalMultiplier);

        float aps = baseSpeed * modifier * typeMul * extraMul;
        if (aps < MinAttacksPerSecond) aps = MinAttacksPerSecond;
        else if (aps > MaxAttacksPerSecond) aps = MaxAttacksPerSecond;

        return 1f / aps;
    }
    
}