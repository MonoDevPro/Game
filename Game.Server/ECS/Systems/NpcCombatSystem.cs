using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class NpcCombatSystem(World world) : GameSystem(world)
{
    [Query]
    [All<AIControlled, Input, NpcAIState, NpcBehavior, NpcTarget, DirtyFlags>]
    private void DriveCombatIntent(
        ref Input input,
        in NpcAIState aiState,
        in NpcBehavior behavior,
        in NpcTarget target,
        ref DirtyFlags dirty)
    {
        bool shouldAttack = aiState.Current == NpcAIStateId.Attacking &&
                            target.HasTarget &&
                            target.DistanceSquared <= behavior.AttackRange * behavior.AttackRange * 1.1f;

        bool isAttacking = (input.Flags & InputFlags.Attack) != 0;
        if (shouldAttack == isAttacking)
            return;

        if (shouldAttack)
        {
            input.InputX = 0;
            input.InputY = 0;
            input.Flags |= InputFlags.Attack;
        }
        else
            input.Flags &= ~InputFlags.Attack;

        dirty.MarkDirty(DirtyComponentType.Input);
    }
}
