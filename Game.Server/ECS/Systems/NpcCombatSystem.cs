using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class NpcCombatSystem(World world, ILogger<NpcCombatSystem>? logger = null) : GameSystem(world)
{
    [Query]
    [All<AIControlled, Input, Position, Facing, NpcAIState, NpcBehavior, NpcTarget, DirtyFlags>]
    private void DriveCombatIntent(
        ref Input input,
        in Position position,
        ref Facing facing,
        in NpcAIState aiState,
        in NpcBehavior behavior,
        in NpcTarget target,
        ref DirtyFlags dirty)
    {
        bool shouldAttack = aiState.Current == NpcAIStateId.Attacking &&
                            target.HasTarget &&
                            target.DistanceSquared <= behavior.AttackRange * behavior.AttackRange * 1.1f;

        bool isAttacking = (input.Flags & InputFlags.BasicAttack) != 0;
        
        // ✅ Atualiza o Facing para apontar para o alvo enquanto está atacando
        if (shouldAttack && target.HasTarget)
        {
            var directionToTarget = PositionLogic.GetDirectionTowards(in position, in target.LastKnownPosition);
            
            if (directionToTarget.X == 0 && directionToTarget.Y == 0)
                return; // Sem direção válida
            
            // Só marca dirty se a direção realmente mudou
            if (facing.DirectionX != directionToTarget.X || 
                facing.DirectionY != directionToTarget.Y)
            {
                facing.DirectionX = directionToTarget.X;
                facing.DirectionY = directionToTarget.Y;
                dirty.MarkDirty(DirtyComponentType.State);
            }
        }
        
        if (shouldAttack == isAttacking)
            return;

        if (shouldAttack)
        {
            input.InputX = 0;
            input.InputY = 0;
            input.Flags |= InputFlags.BasicAttack;
        }
        else
            input.Flags &= ~InputFlags.BasicAttack;

        dirty.MarkDirty(DirtyComponentType.Input);
    }
}