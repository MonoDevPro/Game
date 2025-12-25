using System.Runtime.CompilerServices;
using Game.Domain.Enums;
using GameECS.Shared.Combat.Data;
using GameECS.Shared.Entities.Components;
using GameECS.Shared.Entities.Data;
using DamageType = GameECS.Shared.Combat.Data.DamageType;

namespace GameECS.Shared.Combat.Components;

#region Core Stats Components


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
public struct InCombat { public long LastCombatTick; }

/// <summary>
/// Tag: entidade pode atacar.
/// </summary>
public struct CanAttack { }

/// <summary>
/// Tag: entidade é invulnerável.
/// </summary>
public struct Invulnerable { }

#endregion
