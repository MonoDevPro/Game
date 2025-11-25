using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Logic.Pathfinding;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class NpcNavigationSystem(
    World world,
    IMapService mapService,
    ILogger<NpcNavigationSystem>? logger = null)
    : GameSystem(world)
{
    [Query]
    [All<NavigationAgent, NpcPath, Position, Floor, MapId>]
    public void UpdatePathfinding(
        in Entity entity,
        ref NavigationAgent nav,
        ref NpcPath path,
        in Position pos,
        in Floor floor,
        in MapId mapId)
    {
        // Se não tem destino, limpa caminho
        if (nav.Destination == null)
        {
            if (path.HasPath) path.ClearPath();
            return;
        }

        // Boilerplate de recalculo reduzido:
        // Só recalcula se o alvo se moveu significativamente ou se a flag está ativa
        if (nav.IsPathPending || ShouldRecalculatePath(pos, nav.Destination.Value, path))
        {
            var grid = mapService.GetMapGrid(mapId.Value);
            var spatial = mapService.GetMapSpatial(mapId.Value);
            
            Entity targetEntity = Entity.Null;
            if (World.TryGet(entity, out NpcBrain brain))
            {
                targetEntity = brain.CurrentTarget;
            }

            // Use AStarPathfinder
            var result = AStarPathfinder.FindPath(
                grid,
                spatial,
                entity,
                targetEntity, 
                pos,
                nav.Destination.Value,
                floor.Level,
                ref path,
                allowDiagonal: false);
                
            nav.IsPathPending = false;
        }
    }
    
    private bool ShouldRecalculatePath(Position currentPos, Position dest, NpcPath path)
    {
        if (!path.HasPath) return true;
        if (path.IsPathComplete) return true;
        
        // Check if destination changed significantly from path end
        if (path.WaypointCount > 0)
        {
            var pathEnd = path.GetWaypoint(path.WaypointCount - 1);
            int distSq = (pathEnd.X - dest.X) * (pathEnd.X - dest.X) + (pathEnd.Y - dest.Y) * (pathEnd.Y - dest.Y);
            if (distSq > 2) return true; // Threshold
        }
        
        return false;
    }
}
