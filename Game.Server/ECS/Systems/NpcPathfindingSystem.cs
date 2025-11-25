using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Logic.Pathfinding;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por calcular e atualizar caminhos A* para NPCs.
/// 
/// Funcionalidades:
/// - Calcula paths para NPCs em estados Chasing/Returning/Patrolling
/// - Rate limiting: máximo de recálculos por frame
/// - Recálculo automático quando alvo se move significativamente
/// - Fallback para movimento direto se pathfinding falhar
/// 
/// Ordem de execução: ANTES do NpcMovementSystem
/// </summary>
public sealed partial class NpcPathfindingSystem(
    World world, 
    IMapService mapService, 
    ILogger<NpcPathfindingSystem>? logger = null) : GameSystem(world)
{
    /// <summary>Máximo de recálculos de path por tick (rate limiting)</summary>
    private const int MaxRecalculationsPerTick = 5;
    
    /// <summary>Contador de recálculos no tick atual</summary>
    private int _recalculationsThisTick;
    
    public override void BeforeUpdate(in float deltaTime)
    {
        base.BeforeUpdate(deltaTime);
        _recalculationsThisTick = 0;
    }
    
    /// <summary>
    /// Atualiza o timer de recálculo e verifica necessidade de novo path.
    /// </summary>
    [Query]
    [All<AIControlled, Position, Floor, MapId, NpcAIState, NpcTarget, NpcPatrol, NpcPath>]
    private void UpdatePathfinding(
        in Entity entity,
        in Position position,
        in Floor floor,
        in MapId mapId,
        in NpcAIState aiState,
        in NpcTarget target,
        in NpcPatrol patrol,
        ref NpcPath path,
        [Data] float deltaTime)
    {
        // Atualiza timer
        path.RecalculateTimer -= deltaTime;
        
        // Verifica se precisa calcular um novo path
        bool needsPath = ShouldCalculatePath(in aiState, in target, in patrol, in path, in position);
        
        if (!needsPath)
            return;
        
        // Rate limiting
        if (_recalculationsThisTick >= MaxRecalculationsPerTick)
        {
            // Marca para recálculo no próximo tick
            path.NeedsRecalculation = true;
            return;
        }
        
        // Determina o destino baseado no estado da IA
        Position destination = GetDestinationForState(in aiState, in target, in patrol);
        
        // Se destino é inválido ou muito próximo, limpa o path
        int distSq = (destination.X - position.X) * (destination.X - position.X) + 
                     (destination.Y - position.Y) * (destination.Y - position.Y);
        
        if (distSq <= 1)
        {
            path.ClearPath();
            path.NeedsRecalculation = false;
            path.RecalculateTimer = NpcPath.RecalculateInterval;
            return;
        }
        
        // Obtém o grid do mapa
        var grid = mapService.GetMapGrid(mapId.Value);
        if (grid == null)
        {
            logger?.LogWarning("[NpcPathfinding] Grid não encontrado para MapId={MapId}", mapId.Value);
            path.ClearPath();
            return;
        }
        
        // Calcula o path usando A*
        var result = AStarPathfinder.FindPath(
            grid,
            position,
            destination,
            floor.Level,
            ref path,
            allowDiagonal: false);
        
        _recalculationsThisTick++;
        path.RecalculateTimer = NpcPath.RecalculateInterval;
        
        switch (result)
        {
            case PathfindingResult.Success:
                logger?.LogDebug(
                    "[NpcPathfinding] Entity {EntityId}: Path calculado com {WaypointCount} waypoints para ({DestX},{DestY})",
                    entity.Id, path.WaypointCount, destination.X, destination.Y);
                break;
                
            case PathfindingResult.AlreadyAtDestination:
                path.ClearPath();
                break;
                
            case PathfindingResult.DestinationBlocked:
            case PathfindingResult.NoPath:
            case PathfindingResult.MaxNodesReached:
                // Path não encontrado - NpcMovementSystem usará fallback de movimento direto
                logger?.LogDebug(
                    "[NpcPathfinding] Entity {EntityId}: Pathfinding falhou ({Result}) para ({DestX},{DestY})",
                    entity.Id, result, destination.X, destination.Y);
                path.ClearPath();
                break;
                
            case PathfindingResult.OutOfBounds:
                logger?.LogWarning(
                    "[NpcPathfinding] Entity {EntityId}: Posição atual fora dos limites ({PosX},{PosY})",
                    entity.Id, position.X, position.Y);
                path.ClearPath();
                break;
        }
    }
    
    /// <summary>
    /// Verifica se deve calcular um novo path baseado no estado atual.
    /// </summary>
    private static bool ShouldCalculatePath(
        in NpcAIState aiState,
        in NpcTarget target,
        in NpcPatrol patrol,
        in NpcPath path,
        in Position position)
    {
        // Se marcado explicitamente para recálculo
        if (path.NeedsRecalculation)
            return true;
        
        // Se timer expirou
        if (path.RecalculateTimer <= 0f)
            return true;
        
        // Verifica por estado
        switch (aiState.Current)
        {
            case NpcAIStateId.Chasing:
            case NpcAIStateId.Attacking:
                if (!target.HasTarget)
                    return false;
                
                // Se alvo se moveu significativamente
                int targetDistSq = (target.LastKnownPosition.X - path.LastTargetPosition.X) * 
                                   (target.LastKnownPosition.X - path.LastTargetPosition.X) +
                                   (target.LastKnownPosition.Y - path.LastTargetPosition.Y) * 
                                   (target.LastKnownPosition.Y - path.LastTargetPosition.Y);
                
                if (targetDistSq >= NpcPath.TargetMovedThresholdSq)
                    return true;
                break;
                
            case NpcAIStateId.Returning:
                // Recalcula se não tem path válido
                if (!path.HasPath)
                    return true;
                break;
                
            case NpcAIStateId.Patrolling:
                if (!patrol.HasDestination)
                    return false;
                
                // Recalcula se não tem path válido
                if (!path.HasPath)
                    return true;
                break;
                
            case NpcAIStateId.Idle:
            default:
                // Não precisa de path
                return false;
        }
        
        // Verifica se o path atual ainda é válido (não completou)
        if (path.HasPath && !path.IsPathComplete)
        {
            // Verifica se chegou no waypoint atual
            var currentWaypoint = path.GetCurrentWaypoint();
            if (position.X == currentWaypoint.X && position.Y == currentWaypoint.Y)
            {
                // Não precisa recalcular, apenas avançar (feito no NpcMovementSystem)
                return false;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Obtém a posição destino baseada no estado da IA.
    /// </summary>
    private static Position GetDestinationForState(
        in NpcAIState aiState,
        in NpcTarget target,
        in NpcPatrol patrol)
    {
        return aiState.Current switch
        {
            NpcAIStateId.Chasing => target.LastKnownPosition,
            NpcAIStateId.Attacking => target.LastKnownPosition,
            NpcAIStateId.Returning => patrol.HomePosition,
            NpcAIStateId.Patrolling => patrol.Destination,
            _ => default
        };
    }
}
