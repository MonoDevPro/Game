using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Extensions;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema simples de IA para NPCs melee que perseguem o jogador mais próximo e atacam ao alcance.
/// </summary>
public sealed partial class NpcAISystem(World world, IMapService mapService, ILogger<NpcAISystem>? logger = null)
    : GameSystem(world)
{
    private readonly ILogger<NpcAISystem>? _logger = logger;

    private const int AggroRange = 8;
    private const int SpatialBufferSize = 64;
    private const int MaxChaseDistance = 20; // distância máxima para continuar perseguindo

    [Query]
    [All<AIControlled, Input>]
    [None<Dead>]
    private void ProcessNpc(
        in Entity entity,
        in MapId mapId,
        in Position position,
        ref Input input)
    {
        // Se não tem AttackTarget, tenta adquirir
        if (!World.Has<AttackTarget>(entity))
        {
            var target = AcquireTarget(mapId.Value, position);
            if (target != Entity.Null)
            {
                // checa segurança
                if (World.IsAlive(target) && !World.Has<Dead>(target))
                {
                    World.Add<AttackTarget>(entity, new AttackTarget { TargetEntity = target });
                    _logger?.LogDebug("[NpcAISystem] NPC {Entity} acquired target {Target}", entity.Id, target.Id);
                }
            }
        }

        // Agora, se tem AttackTarget, processa comportamento
        if (World.Has<AttackTarget>(entity))
        {
            ref var attackTarget = ref World.Get<AttackTarget>(entity);
            var target = attackTarget.TargetEntity;
            if (target == Entity.Null || !World.IsAlive(target) || World.Has<Dead>(target))
            {
                input.Flags &= ~InputFlags.Attack;
                World.Remove<AttackTarget>(entity);
                return;
            }

            if (!World.Has<Position>(target))
            {
                // alvo sem posição -- limpar
                input.Flags &= ~InputFlags.Attack;
                World.Remove<AttackTarget>(entity);
                return;
            }

            var targetPosition = World.Get<Position>(target);
            int distance = position.ManhattanDistance(targetPosition);

            // se for muito longe além do chase, largar o target
            if (distance > MaxChaseDistance)
            {
                input.Flags &= ~InputFlags.Attack;
                World.Remove<AttackTarget>(entity);
                _logger?.LogDebug("[NpcAISystem] NPC {Entity} lost target {Target} (too far)", entity.Id, target.Id);
                return;
            }

            if (distance <= SimulationConfig.MaxMeleeAttackRange)
            {
                input.Flags |= InputFlags.Attack;
                input.InputX = 0;
                input.InputY = 0;
                _logger?.LogDebug("[NpcAISystem] NPC {Entity} attacking target {Target}", entity.Id, target.Id);
            }
            else
            {
                // mover em direção
                var dir = PositionLogic.GetDirectionTowards(position, targetPosition);
                input.InputX = dir.x;
                input.InputY = dir.y;
                input.Flags &= ~InputFlags.Attack;
                _logger?.LogDebug("[NpcAISystem] NPC {Entity} moving towards target {Target}", entity.Id, target.Id);
            }
        }
    }

    private Entity AcquireTarget(int mapId, in Position position)
    {
        var spatial = mapService.GetMapSpatial(mapId);
        Span<Entity> stackBuffer = stackalloc Entity[SpatialBufferSize];
        var area = new AreaPosition(
            position.X - AggroRange,
            position.Y - AggroRange,
            position.Z,
            position.X + AggroRange,
            position.Y + AggroRange,
            position.Z);

        int count = spatial.QueryArea(area, stackBuffer);

        Entity best = Entity.Null;
        int bestDistance = int.MaxValue;

        if (count <= stackBuffer.Length)
        {
            for (int i = 0; i < count; i++)
            {
                var candidate = stackBuffer[i];
                if (!World.Has<PlayerControlled>(candidate) || World.Has<Dead>(candidate) || !World.Has<Position>(candidate))
                    continue;

                ref readonly var candidatePos = ref World.Get<Position>(candidate);
                int distance = position.ManhattanDistance(candidatePos);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            return best;
        }

        // caso exceda stack buffer, aluga do pool
        var rented = System.Buffers.ArrayPool<Entity>.Shared.Rent(count);
        try
        {
            Span<Entity> heapSpan = rented.AsSpan(0, count);
            int realCount = spatial.QueryArea(area, heapSpan);
            for (int i = 0; i < realCount; i++)
            {
                var candidate = heapSpan[i];
                if (!World.Has<PlayerControlled>(candidate) 
                    || World.Has<Dead>(candidate) 
                    || !World.Has<Position>(candidate))
                    continue;

                ref readonly var candidatePos = ref World.Get<Position>(candidate);
                int distance = position.ManhattanDistance(candidatePos);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
        }
        finally
        {
            System.Buffers.ArrayPool<Entity>.Shared.Return(rented);
        }
        return best;
    }
}