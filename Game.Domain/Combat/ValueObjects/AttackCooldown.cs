using System.Runtime.CompilerServices;

namespace Game.Domain.ValueObjects.Combat;

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