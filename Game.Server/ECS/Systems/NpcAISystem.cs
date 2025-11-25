using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema de IA de NPCs. Gerencia transições de estado baseadas em:
/// - Proximidade do alvo
/// - Comportamento configurado (Passive, Defensive, Aggressive)
/// - Vocação do NPC (melee vs ranged)
/// 
/// NPCs ranged (Archer, Mage) tentam manter distância ideal do alvo.
/// </summary>
public sealed partial class NpcAISystem(World world, ILogger<NpcAISystem>? logger = null)
    : GameSystem(world)
{
    /// <summary>
    /// Fator de distância mínima para NPCs ranged (porcentagem do range de ataque).
    /// NPCs ranged tentam ficar entre MinDistanceFactor e 1.0 do seu range.
    /// </summary>
    private const float MinDistanceFactor = 0.5f;
    
    [Query]
    [All<AIControlled, Position, NpcBehavior, NpcTarget, NpcPatrol, NpcAIState, NpcInfo>]
    private void UpdateAiState(
        in Entity entity,
        in Position position,
        in NpcBehavior behavior,
        in NpcTarget target,
        in NpcPatrol patrol,
        in NpcInfo npcInfo,
        ref NpcAIState aiState,
        [Data] float deltaTime)
    {
        aiState.Advance(deltaTime);
        
        // Obtém range efetivo baseado na vocação
        int vocationRange = CombatLogic.GetAttackRangeForVocation(npcInfo.VocationId);
        float effectiveRange = MathF.Max(behavior.AttackRange, vocationRange);
        bool isRanged = CombatLogic.IsRangedVocation(npcInfo.VocationId);

        switch (aiState.Current)
        {
            case NpcAIStateId.Idle:
            case NpcAIStateId.Patrolling:
            {
                if (target.HasTarget && behavior.Type != NpcBehaviorType.Passive)
                {
                    logger?.LogDebug("[NpcAI] Entity {EntityId} transitioning to Chasing - TargetNetId={TargetNetId}", 
                        entity.Id, target.TargetNetworkId);
                    Transition(ref aiState, NpcAIStateId.Chasing);
                }
                break;
            }
            case NpcAIStateId.Chasing:
            {
                if (!target.HasTarget)
                {
                    Transition(ref aiState, NpcAIStateId.Returning);
                    break;
                }

                float attackRangeSq = effectiveRange * effectiveRange;
                
                // NPCs ranged: transita para atacar quando estiver em range
                // NPCs melee: precisa estar bem próximo
                if (target.DistanceSquared <= attackRangeSq)
                    Transition(ref aiState, NpcAIStateId.Attacking);
                break;
            }
            case NpcAIStateId.Attacking:
            {
                if (!target.HasTarget)
                {
                    Transition(ref aiState, NpcAIStateId.Returning);
                    break;
                }

                float leashRangeSq = behavior.LeashRange * behavior.LeashRange;
                if (target.DistanceSquared > leashRangeSq)
                {
                    Transition(ref aiState, NpcAIStateId.Returning);
                    break;
                }

                float attackRangeSq = effectiveRange * effectiveRange * 1.2f;
                if (target.DistanceSquared > attackRangeSq)
                {
                    Transition(ref aiState, NpcAIStateId.Chasing);
                    break;
                }
                
                // NPCs ranged: se o alvo chegou muito perto, recuar
                if (isRanged)
                {
                    float minDistanceSq = (effectiveRange * MinDistanceFactor) * (effectiveRange * MinDistanceFactor);
                    if (target.DistanceSquared < minDistanceSq)
                    {
                        // Transita para um estado de "kiting" - implementado como Chasing reverso
                        // O NpcMovementSystem trata isso movendo para longe quando muito perto
                        logger?.LogDebug("[NpcAI] Ranged NPC {EntityId} enemy too close! Distance²: {DistSq}, MinDist²: {MinDistSq}", 
                            entity.Id, target.DistanceSquared, minDistanceSq);
                    }
                }
                break;
            }
            case NpcAIStateId.Returning:
            {
                if (target.HasTarget)
                {
                    Transition(ref aiState, NpcAIStateId.Chasing);
                    break;
                }

                if (position.ManhattanDistance(patrol.HomePosition) <= 0)
                    Transition(ref aiState, NpcAIStateId.Idle);
                break;
            }
        }
    }

    private void Transition(ref NpcAIState state, NpcAIStateId targetState)
    {
        if (!state.TrySetState(targetState))
            return;

        logger?.LogTrace("[NpcAI] State transitioned to {State}", targetState);
    }
}