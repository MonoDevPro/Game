using Game.Domain.Enums;
using GameECS.Shared.Combat.Data;

namespace Game.Domain.ValueObjects.Combat;

public readonly record struct DamageMessage(
    int AttackerId,
    int TargetId,
    DamageType DamageType,
    double RawDamage,
    double DamageAmount,
    bool IsCritical,
    AttackResult Type
)
{
    public double FinalDamage => IsCritical ? (DamageAmount * 1.5) : DamageAmount;
    
    public static DamageMessage Create(
        int attackerId,
        int targetId,
        DamageType damageType,
        double rawDamage,
        double damageAmount,
        bool isCritical,
        AttackResult type)
    {
        return new DamageMessage(
            attackerId,
            targetId,
            damageType,
            rawDamage,
            damageAmount,
            isCritical,
            type);
    }
    
}