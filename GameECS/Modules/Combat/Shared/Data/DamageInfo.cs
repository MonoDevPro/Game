using MemoryPack;

namespace GameECS.Modules.Combat.Shared.Data;

/// <summary>
/// Informações detalhadas sobre um dano aplicado.
/// </summary>
[MemoryPackable]
public readonly partial record struct DamageInfo(
    int RawDamage,
    int FinalDamage,
    int MitigatedDamage,
    DamageType Type,
    bool IsCritical,
    int AttackerId,
    int TargetId,
    long Tick)
{
    public static DamageInfo Create(
        int rawDamage,
        int finalDamage,
        DamageType type,
        bool isCritical,
        int attackerId,
        int targetId,
        long tick)
    {
        return new DamageInfo
        {
            RawDamage = rawDamage,
            FinalDamage = finalDamage,
            MitigatedDamage = rawDamage - finalDamage,
            Type = type,
            IsCritical = isCritical,
            AttackerId = attackerId,
            TargetId = targetId,
            Tick = tick
        };
    }
}

/// <summary>
/// Mensagem de rede para sincronizar dano.
/// </summary>
[MemoryPackable]
public readonly partial record struct DamageNetworkMessage(
    int AttackerId,
    int TargetId,
    int Damage,
    DamageType Type,
    bool IsCritical,
    AttackResult Result,
    long ServerTick);

/// <summary>
/// Mensagem de rede para sincronizar morte.
/// </summary>
[MemoryPackable]
public readonly partial record struct DeathNetworkMessage(
    int EntityId,
    int KillerId,
    long ServerTick);
