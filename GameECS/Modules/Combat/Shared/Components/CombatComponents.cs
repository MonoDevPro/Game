using System.Runtime.CompilerServices;
using GameECS.Modules.Combat.Shared.Data;

namespace GameECS.Modules.Combat.Shared.Components;

#region Core Stats Components

/// <summary>
/// Componente de vida da entidade.
/// </summary>
public struct Health
{
    public int Current;
    public int Maximum;

    public readonly bool IsDead => Current <= 0;
    public readonly bool IsFullHealth => Current >= Maximum;
    public readonly float Percentage => Maximum > 0 ? (float)Current / Maximum : 0f;

    public Health(int max)
    {
        Current = max;
        Maximum = max;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int TakeDamage(int damage)
    {
        int actualDamage = Math.Min(Current, Math.Max(0, damage));
        Current -= actualDamage;
        return actualDamage;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Heal(int amount)
    {
        int actualHeal = Math.Min(Maximum - Current, Math.Max(0, amount));
        Current += actualHeal;
        return actualHeal;
    }

    public void SetMax(int newMax)
    {
        Maximum = Math.Max(1, newMax);
        Current = Math.Min(Current, Maximum);
    }

    public void Reset()
    {
        Current = Maximum;
    }
}

/// <summary>
/// Componente de mana da entidade.
/// </summary>
public struct Mana
{
    public int Current;
    public int Maximum;
    public int RegenPerTick;

    public readonly bool IsEmpty => Current <= 0;
    public readonly bool IsFull => Current >= Maximum;
    public readonly float Percentage => Maximum > 0 ? (float)Current / Maximum : 0f;

    public Mana(int max, int regenPerTick = 1)
    {
        Current = max;
        Maximum = max;
        RegenPerTick = regenPerTick;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryConsume(int amount)
    {
        if (Current >= amount)
        {
            Current -= amount;
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Regenerate()
    {
        Current = Math.Min(Maximum, Current + RegenPerTick);
    }

    public void Reset()
    {
        Current = Maximum;
    }
}

/// <summary>
/// Stats de combate da entidade.
/// </summary>
public struct CombatStats
{
    public int PhysicalDamage;
    public int MagicDamage;
    public int PhysicalDefense;
    public int MagicDefense;
    public int AttackRange;
    public float AttackSpeed;       // Multiplicador de velocidade de ataque
    public float CriticalChance;    // 0-100

    public static CombatStats FromVocation(VocationType vocation)
    {
        var stats = VocationStats.GetForVocation(vocation);
        return new CombatStats
        {
            PhysicalDamage = stats.BasePhysicalDamage,
            MagicDamage = stats.BaseMagicDamage,
            PhysicalDefense = stats.BasePhysicalDefense,
            MagicDefense = stats.BaseMagicDefense,
            AttackRange = stats.BaseAttackRange,
            AttackSpeed = stats.BaseAttackSpeed,
            CriticalChance = stats.BaseCriticalChance
        };
    }
}

/// <summary>
/// Vocação da entidade.
/// </summary>
public struct Vocation
{
    public VocationType Type;
    public int Level;

    public Vocation(VocationType type, int level = 1)
    {
        Type = type;
        Level = level;
    }

    public readonly VocationStats GetBaseStats() => VocationStats.GetForVocation(Type);
    
    public readonly DamageType GetPrimaryDamageType() => Type switch
    {
        VocationType.Mage => DamageType.Magic,
        _ => DamageType.Physical
    };
}

#endregion

#region Attack Components

/// <summary>
/// Requisição de ataque básico.
/// </summary>
public struct AttackRequest
{
    public int TargetEntityId;
    public long RequestTick;

    public static AttackRequest Create(int targetId, long tick) => new()
    {
        TargetEntityId = targetId,
        RequestTick = tick
    };
}

/// <summary>
/// Estado de cooldown de ataque.
/// </summary>
public struct AttackCooldown
{
    public long LastAttackTick;
    public int CooldownTicks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsReady(long currentTick)
        => currentTick >= LastAttackTick + CooldownTicks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetRemainingTicks(long currentTick)
        => Math.Max(0, (int)(LastAttackTick + CooldownTicks - currentTick));

    public void TriggerCooldown(long currentTick, int cooldownTicks)
    {
        LastAttackTick = currentTick;
        CooldownTicks = cooldownTicks;
    }
}

/// <summary>
/// Resultado do último ataque realizado.
/// </summary>
public struct LastAttackResult
{
    public AttackResult Result;
    public int DamageDealt;
    public int TargetEntityId;
    public long Tick;
    public bool WasCritical;
}

/// <summary>
/// Buffer de dano recebido para processamento.
/// </summary>
public struct DamageBuffer
{
    public const int MaxPendingDamages = 8;
    
    public unsafe fixed int DamageValues[MaxPendingDamages];
    public unsafe fixed byte DamageTypes[MaxPendingDamages];
    public unsafe fixed int AttackerIds[MaxPendingDamages];
    public int Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe bool TryAdd(int damage, DamageType type, int attackerId)
    {
        if (Count >= MaxPendingDamages) return false;
        
        DamageValues[Count] = damage;
        DamageTypes[Count] = (byte)type;
        AttackerIds[Count] = attackerId;
        Count++;
        return true;
    }

    public void Clear() => Count = 0;
}

#endregion

#region Tags

/// <summary>
/// Tag: entidade pode participar de combate.
/// </summary>
public struct CombatEntity { }

/// <summary>
/// Tag: entidade está morta.
/// </summary>
public struct Dead { }

/// <summary>
/// Tag: entidade está em combate ativo.
/// </summary>
public struct InCombat
{
    public long LastCombatTick;
}

/// <summary>
/// Tag: entidade pode atacar.
/// </summary>
public struct CanAttack { }

/// <summary>
/// Tag: entidade é invulnerável.
/// </summary>
public struct Invulnerable { }

/// <summary>
/// Tag: entidade está atacando atualmente.
/// </summary>
public struct IsAttacking
{
    public int TargetId;
    public long StartTick;
    public long EndTick;
}

#endregion
