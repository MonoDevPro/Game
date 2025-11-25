using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema de movimento de NPCs. Gera inputs de movimento baseados em:
/// 1. Waypoints do pathfinding A* (NpcPath)
/// 2. Fallback para movimento direto se não houver path
/// 
/// NPCs ranged (Archer, Mage) tentam manter distância ideal do alvo.
/// </summary>
public sealed partial class NpcMovementSystem(World world, ILogger<NpcMovementSystem>? logger = null) : GameSystem(world)
{
    /// <summary>
    /// Fator de distância mínima para NPCs ranged (porcentagem do range de ataque).
    /// </summary>
    private const float MinDistanceFactor = 0.5f;
    
    [Query]
    [All<AIControlled, Input, Position, NpcAIState, NpcTarget, NpcPatrol, NpcBehavior, NpcInfo, NpcPath, DirtyFlags>]
    private void DriveMovement(
        ref Input input,
        in Position position,
        in NpcAIState aiState,
        in NpcTarget target,
        in NpcPatrol patrol,
        in NpcBehavior behavior,
        in NpcInfo npcInfo,
        ref NpcPath path,
        ref DirtyFlags dirty)
    {
        sbyte desiredX = 0;
        sbyte desiredY = 0;
        
        // Obtém range efetivo baseado na vocação
        int vocationRange = CombatLogic.GetAttackRangeForVocation(npcInfo.VocationId);
        float effectiveRange = MathF.Max(behavior.AttackRange, vocationRange);
        bool isRanged = CombatLogic.IsRangedVocation(npcInfo.VocationId);

        switch (aiState.Current)
        {
            case NpcAIStateId.Chasing when target.HasTarget:
            {
                float attackRangeSq = effectiveRange * effectiveRange;
                if (target.DistanceSquared > attackRangeSq)
                {
                    // Tenta usar waypoints do pathfinding
                    if (path.HasPath && !path.IsPathComplete)
                    {
                        (desiredX, desiredY) = GetDirectionToWaypoint(in position, ref path);
                    }
                    else
                    {
                        // Fallback: movimento direto
                        (desiredX, desiredY) = PositionLogic.GetDirectionTowards(in position, target.LastKnownPosition);
                    }
                }
                break;
            }
            case NpcAIStateId.Attacking when target.HasTarget && isRanged:
            {
                // NPCs ranged: se o alvo chegou muito perto, recuar (kiting)
                float minDistanceSq = (effectiveRange * MinDistanceFactor) * (effectiveRange * MinDistanceFactor);
                if (target.DistanceSquared < minDistanceSq)
                {
                    // Move na direção oposta ao alvo
                    var (toTargetX, toTargetY) = PositionLogic.GetDirectionTowards(in position, target.LastKnownPosition);
                    desiredX = (sbyte)(-toTargetX);
                    desiredY = (sbyte)(-toTargetY);
                    
                    // Limpa path quando kiting
                    path.ClearPath();
                    path.RequestRecalculation();
                    
                    logger?.LogDebug("[NpcMovement] Ranged NPC kiting! Moving away from target.");
                }
                // Se está em range ideal, fica parado enquanto ataca
                break;
            }
            case NpcAIStateId.Returning:
            {
                // Tenta usar waypoints do pathfinding
                if (path.HasPath && !path.IsPathComplete)
                {
                    (desiredX, desiredY) = GetDirectionToWaypoint(in position, ref path);
                }
                else
                {
                    // Fallback: movimento direto
                    (desiredX, desiredY) = PositionLogic.GetDirectionTowards(in position, patrol.HomePosition);
                }
                break;
            }
            case NpcAIStateId.Patrolling when patrol.HasDestination:
            {
                // Tenta usar waypoints do pathfinding
                if (path.HasPath && !path.IsPathComplete)
                {
                    (desiredX, desiredY) = GetDirectionToWaypoint(in position, ref path);
                }
                else
                {
                    // Fallback: movimento direto
                    (desiredX, desiredY) = PositionLogic.GetDirectionTowards(in position, patrol.Destination);
                }
                break;
            }
        }

        bool inputChanged = desiredX != input.InputX || desiredY != input.InputY;
        if (inputChanged)
        {
            input.InputX = desiredX;
            input.InputY = desiredY;
            dirty.MarkDirty(DirtyComponentType.Input);
        }
    }
    
    /// <summary>
    /// Calcula direção para o waypoint atual e avança para o próximo se necessário.
    /// </summary>
    private static (sbyte X, sbyte Y) GetDirectionToWaypoint(in Position position, ref NpcPath path)
    {
        var currentWaypoint = path.GetCurrentWaypoint();
        
        // Verifica se chegou no waypoint atual
        if (position.X == currentWaypoint.X && position.Y == currentWaypoint.Y)
        {
            // Avança para o próximo waypoint
            if (!path.AdvanceToNextWaypoint())
            {
                // Caminho completo
                return (0, 0);
            }
            
            currentWaypoint = path.GetCurrentWaypoint();
        }
        
        // Calcula direção para o waypoint
        return PositionLogic.GetDirectionTowards(in position, currentWaypoint);
    }
}