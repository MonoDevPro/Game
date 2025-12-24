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