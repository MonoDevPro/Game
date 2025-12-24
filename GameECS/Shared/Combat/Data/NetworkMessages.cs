using MemoryPack;

namespace GameECS.Shared.Combat.Data;

/// <summary>
/// Mensagem de rede para sincronizar dano.
/// </summary>
[MemoryPackable]
public readonly partial record struct DamageMessage(
    int AttackerId,
    int TargetId,
    DamageType Type,
    int RawDamage,
    int FinalDamage,
    int MitigatedDamage,
    bool IsCritical,
    AttackResult Result)
{
    public static DamageMessage Create(
        int attackerId,
        int targetId,
        DamageType type,
        int rawDamage,
        int finalDamage,
        bool isCritical,
        AttackResult result)
    {
        return new DamageMessage
        {
            AttackerId = attackerId,
            TargetId = targetId,
            Type = type,
            RawDamage = rawDamage,
            FinalDamage = finalDamage,
            MitigatedDamage = rawDamage - finalDamage,
            IsCritical = isCritical,
            Result = result
        };
    }
    
    public static DamageMessage Create(AttackResult result)
    {
        return new DamageMessage
        {
            Result = result
        };
    }
}

/// <summary>
/// Mensagem de rede para sincronizar morte.
/// </summary>
[MemoryPackable]
public readonly partial record struct DeathMessage(
    int EntityId,
    int KillerId);