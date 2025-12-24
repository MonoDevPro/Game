using System.Runtime.CompilerServices;

namespace GameECS.Server.Combat.Components;

/// <summary>
/// Estado de ataque no servidor (tick-based).
/// </summary>
public struct ServerAttackState
{
    public int TargetEntityId;
    public long StartTick;
    public long CompletionTick;
    public bool IsAttacking;
    public int PendingDamage;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool ShouldComplete(long currentTick)
        => IsAttacking && currentTick >= CompletionTick;

    public void StartAttack(int targetId, long currentTick, int attackDurationTicks, int damage)
    {
        TargetEntityId = targetId;
        StartTick = currentTick;
        CompletionTick = currentTick + attackDurationTicks;
        IsAttacking = true;
        PendingDamage = damage;
    }

    public void Complete()
    {
        IsAttacking = false;
        PendingDamage = 0;
    }

    public void Cancel()
    {
        IsAttacking = false;
        PendingDamage = 0;
        TargetEntityId = -1;
    }
}

/// <summary>
/// Configuração de combate específica do servidor para cada entidade.
/// </summary>
public struct ServerCombatConfig
{
    /// <summary>
    /// Duração do ataque em ticks (tempo de animação).
    /// </summary>
    public int AttackDurationTicks;
    
    /// <summary>
    /// Tick em que o dano é aplicado durante o ataque (para sincronização de animação).
    /// </summary>
    public int DamageApplicationTick;
    
    /// <summary>
    /// Se a entidade pode ser interrompida durante ataque.
    /// </summary>
    public bool CanBeInterrupted;

    public static ServerCombatConfig Default => new()
    {
        AttackDurationTicks = 20,
        DamageApplicationTick = 10,
        CanBeInterrupted = true
    };

    public static ServerCombatConfig Knight => new()
    {
        AttackDurationTicks = 25,
        DamageApplicationTick = 15,
        CanBeInterrupted = false  // Knight não é interrompido
    };

    public static ServerCombatConfig Mage => new()
    {
        AttackDurationTicks = 35,
        DamageApplicationTick = 25,  // Dano vem no final do cast
        CanBeInterrupted = true
    };

    public static ServerCombatConfig Archer => new()
    {
        AttackDurationTicks = 15,
        DamageApplicationTick = 10,
        CanBeInterrupted = true
    };
}

/// <summary>
/// Estado de alvo atual para ataque automático.
/// </summary>
public struct TargetLock
{
    public int TargetEntityId;
    public long LockStartTick;
    public bool IsLocked;

    public void Lock(int targetId, long tick)
    {
        TargetEntityId = targetId;
        LockStartTick = tick;
        IsLocked = true;
    }

    public void Release()
    {
        TargetEntityId = -1;
        IsLocked = false;
    }
}

/// <summary>
/// Estatísticas de combate para o servidor.
/// </summary>
public struct CombatStatistics
{
    public int TotalDamageDealt;
    public int TotalDamageReceived;
    public int TotalKills;
    public int TotalDeaths;
    public int CriticalHits;
    public int AttacksMade;
    public int AttacksReceived;

    public void RecordDamageDealt(int damage, bool isCritical)
    {
        TotalDamageDealt += damage;
        AttacksMade++;
        if (isCritical) CriticalHits++;
    }

    public void RecordDamageReceived(int damage)
    {
        TotalDamageReceived += damage;
        AttacksReceived++;
    }

    public void RecordKill() => TotalKills++;
    public void RecordDeath() => TotalDeaths++;
}
