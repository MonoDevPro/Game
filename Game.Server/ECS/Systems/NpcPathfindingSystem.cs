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
    private const int MaxRecalculationsPerTick = 1;
    
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
        // Atualiza timers
        path.RecalculateTimer -= deltaTime;
        
        // Incrementa stuck timer se tem path ativo
        if (path.HasPath && !path.IsPathComplete)
            path.StuckTimer += deltaTime;
        
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
        
        logger?.LogDebug(
            "[NpcPathfinding] Entity {EntityId}: State={State}, HasTarget={HasTarget}, TargetPos=({TX},{TY}), Destination=({DX},{DY})",
            entity.Id, aiState.Current, target.HasTarget, 
            target.LastKnownPosition.X, target.LastKnownPosition.Y,
            destination.X, destination.Y);
        
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
        
        // Obtém o grid e spatial do mapa
        var grid = mapService.GetMapGrid(mapId.Value);
        var spatial = mapService.GetMapSpatial(mapId.Value);
        
        // Calcula o path usando A* (considerando outras entidades como obstáculos)
        var result = AStarPathfinder.FindPath(
            grid,
            spatial,
            entity,           // Entidade fonte (será ignorada como obstáculo)
            target.Target,    // Entidade alvo (será ignorada como obstáculo)
            position,
            destination,
            floor.Level,
            ref path,
            allowDiagonal: false);
        
        _recalculationsThisTick++;
        path.RecalculateTimer = NpcPath.RecalculateInterval;
        path.NeedsRecalculation = false;
        
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
        // PRIMEIRO: Verifica estado - estados que não precisam de path
        // Isso DEVE vir antes de qualquer outra verificação para evitar
        // calcular paths para destinos inválidos (0,0)
        if (aiState.Current is NpcAIStateId.Idle)
            return false;
        
        // Se marcado explicitamente para recálculo (primeira vez ou forçado)
        if (path.NeedsRecalculation)
            return true;
        
        // Se está stuck (não progride no path), força recálculo
        if (path.IsStuck)
            return true;
        
        // Para Chasing/Attacking, precisa ter alvo
        if (aiState.Current is NpcAIStateId.Chasing or NpcAIStateId.Attacking)
        {
            if (!target.HasTarget)
                return false;
            
            // Se não tem path válido, precisa calcular
            if (!path.HasPath)
                return true;
            
            // Se path completou, precisa recalcular
            if (path.IsPathComplete)
                return true;
            
            // Se timer não expirou e tem path válido, não recalcula
            if (path.RecalculateTimer > 0f)
                return false;
            
            // Timer expirou - verifica se alvo se moveu significativamente
            // Compara destino ATUAL do path com posição ATUAL do alvo
            var currentDestination = path.GetWaypoint(path.WaypointCount - 1);
            int targetDistSq = (target.LastKnownPosition.X - currentDestination.X) * 
                               (target.LastKnownPosition.X - currentDestination.X) +
                               (target.LastKnownPosition.Y - currentDestination.Y) * 
                               (target.LastKnownPosition.Y - currentDestination.Y);
            
            return targetDistSq >= NpcPath.TargetMovedThresholdSq;
        }
        
        // Para Returning/Patrolling
        if (aiState.Current is NpcAIStateId.Returning or NpcAIStateId.Patrolling)
        {
            // Se não tem path válido, precisa calcular
            if (!path.HasPath)
                return true;
            
            // Se path completou, pode precisar recalcular
            return path.IsPathComplete;
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
