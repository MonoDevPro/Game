using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.DTOs.Game.Npc;
using Game.DTOs.Game.Player;
using Game.ECS.Components;
using Game.ECS.Events;
using Game.ECS.Services.Map;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema de IA para NPCs. Gerencia a máquina de estados e define destinos de navegação.
/// Estados: Idle → Patrol → Chase → Combat → ReturnHome
/// </summary>
public sealed partial class NpcAISystem(World world, MapIndex mapIndex, ILogger<NpcAISystem>? logger = null)
    : GameSystem(world, logger)
{
    private readonly Random _random = new();
    
    // Buffers for entity queries
    private readonly Entity[] _nearbyEntities = new Entity[32];

    /// <summary>
    /// Inicializa NPCs sem Brain com estado padrão.
    /// </summary>
    [Query]
    [All<AIControlled, AIBehaviour, Position>]
    [None<Brain, Dead>]
    private void InitializeBrain(
        in Entity entity,
        in Position position)
    {
        World.Add(entity, new Brain
        {
            CurrentState = AIState.Idle,
            StateTimer = 0f,
            CurrentTarget = Entity.Null
        });
        
        // Também inicializa NavigationAgent se não existir
        if (!World.Has<NavigationAgent>(entity))
        {
            World.Add(entity, new NavigationAgent
            {
                Destination = null,
                StoppingDistance = 0f,
                IsPathPending = false
            });
        }
    }

    /// <summary>
    /// Atualiza a máquina de estados e comportamento dos NPCs.
    /// </summary>
    [Query]
    [All<AIControlled, Brain, AIBehaviour, Position, MapId, Direction, Speed>]
    [None<Dead>]
    private void UpdateAI(
        in Entity entity,
        ref Brain brain,
        in AIBehaviour behaviour,
        in Position position,
        in MapId mapId,
        ref Direction direction,
        ref Speed speed,
        [Data] float deltaTime)
    {
        brain.StateTimer += deltaTime;
        
        switch (brain.CurrentState)
        {
            case AIState.Idle:
                ProcessIdleState(entity, ref brain, behaviour, position, mapId, deltaTime);
                break;
            case AIState.Patrol:
                ProcessPatrolState(entity, ref brain, behaviour, position, mapId, ref direction, ref speed, deltaTime);
                break;
            case AIState.Chase:
                ProcessChaseState(entity, ref brain, behaviour, position, mapId, ref direction, ref speed, deltaTime);
                break;
            case AIState.Combat:
                ProcessCombatState(entity, ref brain, behaviour, position, mapId, ref direction, ref speed, deltaTime);
                break;
            case AIState.ReturnHome:
                ProcessReturnHomeState(entity, ref brain, behaviour, position, mapId, ref direction, ref speed, deltaTime);
                break;
        }
    }

    #region State Handlers
    
    private void ProcessIdleState(
        Entity entity,
        ref Brain brain,
        in AIBehaviour behaviour,
        in Position position,
        in MapId mapId,
        [Data] in float deltaTime)
    {
        // Check for nearby players (aggression)
        if (behaviour.Type == BehaviorType.Aggressive)
        {
            if (TryFindNearestPlayer(position, mapId.Value, behaviour.VisionRange, out var player))
            {
                brain.CurrentTarget = player;
                ChangeState(entity, ref brain, AIState.Chase);
                return;
            }
        }
        
        // Random idle duration
        float idleDuration = behaviour.IdleDurationMin + 
            (float)_random.NextDouble() * (behaviour.IdleDurationMax - behaviour.IdleDurationMin);
        
        if (brain.StateTimer >= idleDuration)
        {
            // Chance to patrol
            if (behaviour.PatrolRadius > 0 && _random.NextDouble() < 0.5)
            {
                ChangeState(entity, ref brain, AIState.Patrol);
            }
            else
            {
                brain.StateTimer = 0f; // Reset idle timer
            }
        }
    }
    
    private void ProcessPatrolState(
        Entity entity,
        ref Brain brain,
        in AIBehaviour behaviour,
        in Position position,
        in MapId mapId,
        ref Direction direction,
        ref Speed speed,
        [Data] in float deltaTime)
    {
        // Check for nearby players during patrol
        if (behaviour.Type == BehaviorType.Aggressive)
        {
            if (TryFindNearestPlayer(position, mapId.Value, behaviour.VisionRange, out var player))
            {
                brain.CurrentTarget = player;
                ChangeState(entity, ref brain, AIState.Chase);
                return;
            }
        }
        
        // Get current home (spawn) position for patrol center
        var homePos = GetHomePosition(entity, position);

        // Get or set patrol destination
        if (!World.TryGet<NavigationAgent>(entity, out var nav))
            return;

        // If a stale destination exists (e.g. from an interrupted patrol), validate it first.
        if (nav.Destination != null)
        {
            var dest = ClampToMap(mapId.Value, nav.Destination.Value);
            if (!dest.Equals(nav.Destination.Value))
            {
                ref var navRef = ref World.Get<NavigationAgent>(entity);
                navRef.Destination = dest;
                nav = navRef;
            }

            // If clamping collapses to current position, reset destination to pick a new one.
            if (IsAtPosition(position, dest))
            {
                ref var navRef = ref World.Get<NavigationAgent>(entity);
                navRef.Destination = null;
                nav = navRef;
            }
        }

        if (nav.Destination == null)
        {
            // Pick random patrol point around spawn/home
            var patrolPos = ClampToMap(mapId.Value, GetRandomPatrolPosition(homePos, (int)behaviour.PatrolRadius));
            ref var navRef = ref World.Get<NavigationAgent>(entity);
            navRef.Destination = patrolPos;
            nav = navRef;
        }
        
        // Move towards destination
        if (nav.Destination != null)
        {
            var destination = ClampToMap(mapId.Value, nav.Destination.Value);
            MoveTowards(ref direction, ref speed, position, destination, 2f);
            
            // Check if reached destination
            if (IsAtPosition(position, destination))
            {
                ref var navRef = ref World.Get<NavigationAgent>(entity);
                navRef.Destination = null;
                ChangeState(entity, ref brain, AIState.Idle);
            }
        }
        
        // Timeout patrol
        if (brain.StateTimer > 10f)
        {
            ChangeState(entity, ref brain, AIState.Idle);
        }
    }
    
    private void ProcessChaseState(
        Entity entity,
        ref Brain brain,
        in AIBehaviour behaviour,
        in Position position,
        in MapId mapId,
        ref Direction direction,
        ref Speed speed,
        [Data] in float deltaTime)
    {
        // Check if target still valid
        if (brain.CurrentTarget == Entity.Null || !World.IsAlive(brain.CurrentTarget))
        {
            brain.CurrentTarget = Entity.Null;
            ChangeState(entity, ref brain, AIState.ReturnHome);
            return;
        }
        
        // Check if target is dead
        if (World.Has<Dead>(brain.CurrentTarget))
        {
            brain.CurrentTarget = Entity.Null;
            ChangeState(entity, ref brain, AIState.ReturnHome);
            return;
        }
        
        // Get target position
        if (!World.TryGet<Position>(brain.CurrentTarget, out var targetPos))
        {
            brain.CurrentTarget = Entity.Null;
            ChangeState(entity, ref brain, AIState.ReturnHome);
            return;
        }

        // Drop target if it is on another map
        if (World.TryGet<MapId>(brain.CurrentTarget, out var targetMapId) && targetMapId.Value != mapId.Value)
        {
            brain.CurrentTarget = Entity.Null;
            ChangeState(entity, ref brain, AIState.ReturnHome);
            return;
        }

        // Drop target if it's out of vision range (lost sight)
        float distanceToTarget = CalculateDistance(position, targetPos);
        if (distanceToTarget > behaviour.VisionRange)
        {
            brain.CurrentTarget = Entity.Null;
            ChangeState(entity, ref brain, AIState.ReturnHome);
            return;
        }
        
        // Check leash range
        float distanceFromHome = CalculateDistance(position, GetHomePosition(entity, position));
        if (distanceFromHome > behaviour.LeashRange)
        {
            brain.CurrentTarget = Entity.Null;
            ChangeState(entity, ref brain, AIState.ReturnHome);
            return;
        }
        
        // Check if in attack range
        if (distanceToTarget <= behaviour.AttackRange)
        {
            ChangeState(entity, ref brain, AIState.Combat);
            return;
        }
        
        // Move towards target
        MoveTowards(ref direction, ref speed, position, targetPos, 3f);
        
        // Timeout chase
        if (brain.StateTimer > 30f)
        {
            brain.CurrentTarget = Entity.Null;
            ChangeState(entity, ref brain, AIState.ReturnHome);
        }
    }
    
    private void ProcessCombatState(
        Entity entity,
        ref Brain brain,
        in AIBehaviour behaviour,
        in Position position,
        in MapId mapId,
        ref Direction direction,
        ref Speed speed,
        [Data] in float deltaTime)
    {
        // Check if target still valid
        if (brain.CurrentTarget == Entity.Null || !World.IsAlive(brain.CurrentTarget))
        {
            brain.CurrentTarget = Entity.Null;
            ChangeState(entity, ref brain, AIState.ReturnHome);
            return;
        }
        
        if (World.Has<Dead>(brain.CurrentTarget))
        {
            brain.CurrentTarget = Entity.Null;
            ChangeState(entity, ref brain, AIState.Idle);
            return;
        }
        
        // Get target position
        if (!World.TryGet<Position>(brain.CurrentTarget, out var targetPos))
        {
            brain.CurrentTarget = Entity.Null;
            ChangeState(entity, ref brain, AIState.ReturnHome);
            return;
        }

        // Drop target if it is on another map
        if (World.TryGet<MapId>(brain.CurrentTarget, out var targetMapId) && targetMapId.Value != mapId.Value)
        {
            brain.CurrentTarget = Entity.Null;
            ChangeState(entity, ref brain, AIState.ReturnHome);
            return;
        }
        
        // Check distance
        float distanceToTarget = CalculateDistance(position, targetPos);

        // Lost sight while in combat -> return home
        if (distanceToTarget > behaviour.VisionRange)
        {
            brain.CurrentTarget = Entity.Null;
            ChangeState(entity, ref brain, AIState.ReturnHome);
            return;
        }
        
        // If too far, chase again
        if (distanceToTarget > behaviour.AttackRange * 1.5f)
        {
            ChangeState(entity, ref brain, AIState.Chase);
            return;
        }
        
        // Stop moving during combat
        speed.Value = 0f;
        
        // Face target
        direction.X = (sbyte)Math.Sign(targetPos.X - position.X);
        direction.Y = (sbyte)Math.Sign(targetPos.Y - position.Y);
        
        // Attack (create attack command if ready)
        if (!World.Has<AttackCommand>(entity))
        {
            // Check cooldown
            if (World.TryGet<CombatState>(entity, out var combatState) && !combatState.InCooldown)
            {
                var vocationId = World.Has<VocationId>(entity) ? World.Get<VocationId>(entity).Value : (byte)1;
                var attackStyle = GetAttackStyleFromVocation(vocationId);
                
                World.Add(entity, new AttackCommand
                {
                    Target = brain.CurrentTarget,
                    TargetPosition = targetPos,
                    Style = attackStyle,
                    ConjureDuration = 1f, // TODO: Placeholder, can be adjusted per style
                });
            }
        }
    }
    
    private void ProcessReturnHomeState(
        Entity entity,
        ref Brain brain,
        in AIBehaviour behaviour,
        in Position position,
        in MapId mapId,
        ref Direction direction,
        ref Speed speed,
        [Data] in float deltaTime)
    {
        var homePos = ClampToMap(mapId.Value, GetHomePosition(entity, position));
        
        // Check if reached home
        if (IsAtPosition(position, homePos))
        {
            ChangeState(entity, ref brain, AIState.Idle);
            return;
        }
        
        // Move towards home
        MoveTowards(ref direction, ref speed, position, homePos, 3f);
        
        // Timeout return
        if (brain.StateTimer > 15f)
        {
            ChangeState(entity, ref brain, AIState.Idle);
        }
    }
    
    #endregion
    
    #region Helpers
    
    private void ChangeState(Entity entity, ref Brain brain, AIState newState)
    {
        var oldState = brain.CurrentState;
        brain.CurrentState = newState;
        brain.StateTimer = 0f;

        // Reset navigation destination when switching contexts.
        // This avoids carrying a stale/off-bounds patrol destination through ReturnHome -> Idle -> Patrol.
        if (World.Has<NavigationAgent>(entity))
        {
            ref var nav = ref World.Get<NavigationAgent>(entity);

            if (newState is AIState.Patrol or AIState.ReturnHome)
            {
                nav.Destination = null;
                nav.IsPathPending = false;
            }
        }
        
        // Fire state change event
        var stateEvent = new NpcStateChangedEvent(entity, oldState, newState);
        EventBus.Send(ref stateEvent);
        
        logger?.LogDebug("[NpcAI] Entity {Entity} changed state: {OldState} -> {NewState}", 
            entity, oldState, newState);
    }
    
    private bool TryFindNearestPlayer(Position center, int mapId, float range, out Entity player)
    {
        player = Entity.Null;
        
        if (!mapIndex.HasMap(mapId))
            return false;
        
        var spatial = mapIndex.GetMapSpatial(mapId);
        
        // Query area around NPC
        var min = new Position { X = center.X - (int)range, Y = center.Y - (int)range };
        var max = new Position { X = center.X + (int)range, Y = center.Y + (int)range };
        
        int count = spatial.QueryArea(min, max,  _nearbyEntities);
        
        float nearestDist = float.MaxValue;
        
        for (int i = 0; i < count; i++)
        {
            var entity = _nearbyEntities[i];
            if (!World.IsAlive(entity))
                continue;
            if (!World.Has<PlayerControlled>(entity))
                continue;
            if (World.Has<Dead>(entity))
                continue;
            
            if (!World.TryGet<Position>(entity, out var entityPos))
                continue;
            
            float dist = CalculateDistance(center, entityPos);
            if (dist < nearestDist && dist <= range)
            {
                nearestDist = dist;
                player = entity;
            }
        }
        
        return player != Entity.Null;
    }
    
    private Position GetRandomPatrolPosition(Position home, int radius)
    {
        int offsetX = _random.Next(-radius, radius + 1);
        int offsetY = _random.Next(-radius, radius + 1);
        
        return new Position
        {
            X = home.X + offsetX,
            Y = home.Y + offsetY
        };
    }
    
    private Position GetHomePosition(Entity entity, Position currentPos)
    {
        // Try to get spawn point
        if (World.TryGet<SpawnPoint>(entity, out var spawnPoint))
        {
            return new Position { X = spawnPoint.X, Y = spawnPoint.Y, Z = spawnPoint.Z };
        }
        
        // Fall back to current position
        return currentPos;
    }
    
    private void MoveTowards(ref Direction direction, ref Speed speed, Position from, Position to, float moveSpeed)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;
        
        direction.X = (sbyte)Math.Sign(dx);
        direction.Y = (sbyte)Math.Sign(dy);
        
        if (dx != 0 || dy != 0)
        {
            speed.Value = moveSpeed;
        }
        else
        {
            speed.Value = 0f;
        }
    }
    
    private static bool IsAtPosition(Position a, Position b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;

    private Position ClampToMap(int mapId, Position position)
    {
        if (!mapIndex.HasMap(mapId))
            return position;
        return mapIndex.GetMapGrid(mapId).ClampToBounds(position);
    }
    
    private static float CalculateDistance(Position a, Position b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
    
    private static AttackStyle GetAttackStyleFromVocation(byte vocationId)
    {
        return vocationId switch
        {
            1 => AttackStyle.Melee,
            2 => AttackStyle.Ranged,
            3 => AttackStyle.Magic,
            _ => AttackStyle.Melee
        };
    }
    
    #endregion
}
