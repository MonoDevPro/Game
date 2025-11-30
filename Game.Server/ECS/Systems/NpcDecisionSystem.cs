using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Domain.Templates;
using Game.ECS.Components;
using Game.ECS.Schema.Components;
using Game.ECS.Services;
using Game.ECS.Systems;
using Game.Server.Npc;

namespace Game.Server.ECS.Systems;

public sealed partial class NpcDecisionSystem(
    World world,
    INpcRepository npcRepository,
    IMapService? mapService)
    : GameSystem(world, mapService)
{
    private const int MaxSpatialResults = 32;
    private readonly INpcRepository _npcRepository = npcRepository;
    private readonly IMapService? _mapService = mapService;

    [Query]
    [All<NpcBrain, Position, NpcType, NavigationAgent, Floor, MapId, NpcPatrol>]
    public void UpdateBehavior(
        in Entity entity,
        ref NpcBrain brain,
        ref NavigationAgent nav,
        in Position pos,
        in NpcType type,
        in Floor floor,
        in MapId mapId,
        in NpcPatrol patrol)
    {
        // Busca configuração estática (Flyweight) via Singleton ou Serviço injetado
        var template = npcRepository.GetTemplate(type.TemplateId);
        var config = template.Behavior;

        // Máquina de Estados Simplificada
        switch (brain.CurrentState)
        {
            case NpcState.Idle:
                if (SearchForTarget(entity, pos, floor, mapId, config.VisionRange, out var target))
                {
                    brain.CurrentTarget = target;
                    brain.CurrentState = NpcState.Chase;
                }
                break;

            case NpcState.Chase:
                // Decisão Pura: "Onde eu quero estar?"
                // Não calcula pathfinding aqui, apenas define o destino final.
                if (World.IsAlive(brain.CurrentTarget) && World.TryGet(brain.CurrentTarget, out Position targetPos))
                {
                    nav.Destination = targetPos;
                    nav.StoppingDistance = config.AttackRange;
                    
                    // Check if target is too far (Leash)
                    float distSq = DistanceSquared(pos, targetPos);
                    if (distSq > config.LeashRange * config.LeashRange)
                    {
                         brain.CurrentTarget = Entity.Null;
                         brain.CurrentState = NpcState.ReturnHome;
                    }
                }
                else
                {
                    brain.CurrentTarget = Entity.Null;
                    brain.CurrentState = NpcState.ReturnHome;
                }
                break;
                
            case NpcState.ReturnHome:
                nav.Destination = patrol.HomePosition;
                nav.StoppingDistance = 0.1f;
                
                if (DistanceSquared(pos, patrol.HomePosition) < 1f)
                {
                    brain.CurrentState = NpcState.Idle;
                    nav.Destination = null;
                }
                break;
        }
    }

    private bool SearchForTarget(Entity self, Position position, Floor floor, MapId mapId, float visionRange, out Entity target)
    {
        target = Entity.Null;
        var spatial = _mapService?.GetMapSpatial(mapId.Value);
        if (spatial == null) return false;
        
        int radius2D = Math.Max(1, (int)MathF.Ceiling(visionRange));
        var center = new SpatialPosition(position.X, position.Y, floor.Value);

        Span<Entity> results = stackalloc Entity[MaxSpatialResults];
        int found = spatial.QueryCircle(center, radius2D, results);

        float bestDistance = float.MaxValue;

        for (int i = 0; i < found; i++)
        {
            var candidate = results[i];
            if (candidate == self) continue;
            if (!World.IsAlive(candidate)) continue;
            if (!World.Has<PlayerControlled>(candidate)) continue; // Only target players for now
            if (World.Has<Dead>(candidate)) continue;
            if (!World.TryGet(candidate, out MapId candidateMap) || candidateMap.Value != mapId.Value) continue;
            if (!World.TryGet(candidate, out Floor candidateFloor) || candidateFloor.Value != floor.Value) continue;
            if (!World.TryGet(candidate, out Position candidatePosition)) continue;

            float distanceSq = DistanceSquared(position, candidatePosition);
            if (distanceSq > visionRange * visionRange)
                continue;

            if (distanceSq >= bestDistance)
                continue;

            target = candidate;
            bestDistance = distanceSq;
        }

        return target != Entity.Null;
    }

    private static float DistanceSquared(Position a, Position b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
