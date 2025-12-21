namespace GameECS.Modules.Combat.Shared.Data;

/// <summary>
/// Informações detalhadas sobre um dano aplicado.
/// </summary>
public readonly struct DamageInfo
{
    public int RawDamage { get; init; }
    public int FinalDamage { get; init; }
    public int MitigatedDamage { get; init; }
    public DamageType Type { get; init; }
    public bool IsCritical { get; init; }
    public int AttackerId { get; init; }
    public int TargetId { get; init; }
    public long Tick { get; init; }

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
public readonly struct DamageNetworkMessage
{
    public int AttackerId { get; init; }
    public int TargetId { get; init; }
    public int Damage { get; init; }
    public DamageType Type { get; init; }
    public bool IsCritical { get; init; }
    public AttackResult Result { get; init; }
    public long ServerTick { get; init; }
}

/// <summary>
/// Mensagem de rede para sincronizar morte.
/// </summary>
public readonly struct DeathNetworkMessage
{
    public int EntityId { get; init; }
    public int KillerId { get; init; }
    public long ServerTick { get; init; }
}
