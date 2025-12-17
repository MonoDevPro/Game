using System. Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Arch. System.SourceGenerator;
using Game.DTOs. Game. Npc;
using Game.ECS.Components;
using Game.ECS.Helpers;
using Game.ECS. Services.Map;
using Microsoft. Extensions.Logging;

namespace Game.ECS.Systems;

public sealed partial class AISystem(
    World world,
    MapIndex mapIndex,
    ILogger<AISystem>? logger = null)
    : GameSystem(world, logger)
{
    private readonly Random _random = new();

    [Query]
    [All<Brain, AIBehaviour, NavigationAgent, Position, SpawnPoint, Direction, Speed, MapId, CombatStats, CombatState, Walkable>]
    [None<Dead>]
    private void UpdateAI(
        in Entity entity,
        ref Brain brain,
        ref NavigationAgent navigation,
        ref Direction dir,
        ref Speed speed,
        ref CombatState combatState,
        in AIBehaviour behaviour,
        in Position pos,
        in Walkable walk,
        in SpawnPoint spawn,
        in MapId mapId,
        in CombatStats combatStats,
        [Data] float deltaTime)
    {
        // Atualiza temporizador de estado
        if (brain.StateTimer > 0)
            brain.StateTimer -= deltaTime;

        // Valida alvo atual (pode ter morrido ou sido destruído)
        ValidateCurrentTarget(ref brain);

        // Lógica da Máquina de Estados
        switch (brain.CurrentState)
        {
            case AIState. Idle:
                HandleIdle(ref brain, ref navigation, ref dir, ref speed, in behaviour);
                break;

            case AIState. Patrol:
                HandlePatrol(ref brain, ref navigation, ref dir, ref speed, in behaviour, in pos, in walk, in spawn);
                break;

            case AIState. Chase:
                HandleChase(ref brain, ref navigation, ref dir, ref speed, in behaviour, in pos, in walk, in spawn);
                break;

            case AIState.ReturnHome:
                HandleReturnHome(ref brain, ref navigation, ref dir, ref speed, in behaviour, in pos, in walk, in spawn);
                break;
                
            case AIState.Combat:
                HandleCombat(entity, ref brain, ref navigation, ref dir, ref speed, ref combatState, 
                    in behaviour, in pos, in spawn, in combatStats, deltaTime);
                break;
        }

        // Sensor Global:  Verifica se deve interromper o estado atual para perseguir alguém
        if (brain.CurrentState != AIState.Chase && 
            brain.CurrentState != AIState.Combat && 
            brain.CurrentState != AIState.ReturnHome)
        {
            ScanForTargets(ref brain, in behaviour, in pos, in spawn, in mapId);
        }
    }

    /// <summary>
    /// Valida se o alvo atual ainda é válido (existe e está vivo).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateCurrentTarget(ref Brain brain)
    {
        if (brain.CurrentTarget == Entity.Null)
            return;

        // Verifica se a entidade ainda existe
        if (!World.IsAlive(brain.CurrentTarget))
        {
            brain.CurrentTarget = Entity.Null;
            if (brain.CurrentState == AIState.Chase || brain.CurrentState == AIState.Combat)
            {
                brain.CurrentState = AIState.ReturnHome;
            }
            return;
        }

        // Verifica se o alvo está morto
        if (World.Has<Dead>(brain.CurrentTarget))
        {
            brain.CurrentTarget = Entity.Null;
            if (brain.CurrentState == AIState.Chase || brain.CurrentState == AIState.Combat)
            {
                brain.CurrentState = AIState.ReturnHome;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleIdle(ref Brain brain, ref NavigationAgent navigation, ref Direction dir, ref Speed speed, in AIBehaviour behaviour)
    {
        // Para o movimento
        dir. X = 0;
        dir.Y = 0;
        speed.Value = 0f;
        navigation.Destination = null;
        navigation.IsPathPending = false;

        // Se o tempo acabou, decide o que fazer
        if (brain.StateTimer <= 0)
        {
            // 50% chance de patrulhar
            if (_random. NextDouble() > 0.5)
            {
                brain.CurrentState = AIState.Patrol;
                brain.StateTimer = 2.0f; // Tempo inicial de patrulha
            }
            else
            {
                brain.StateTimer = RandomRange(behaviour.IdleDurationMin, behaviour.IdleDurationMax);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandlePatrol(
        ref Brain brain, 
        ref NavigationAgent navigation,
        ref Direction dir, 
        ref Speed speed,
        in AIBehaviour behaviour, 
        in Position pos, 
        in Walkable walk,
        in SpawnPoint spawn)
    {
        speed.Value = ComputeCellsPerSecond(in walk, isSprinting: false);
        navigation.StoppingDistance = 0f;
        
        float distToSpawn = Distance(pos, new Position { X = spawn.X, Y = spawn.Y, Z = spawn.Z });
        
        // Se afastou demais do spawn, volta
        if (distToSpawn > behaviour.PatrolRadius)
        {
            brain.CurrentState = AIState. ReturnHome;
            return;
        }
        
        // Muda de direção aleatoriamente se o timer zerar
        if (brain.StateTimer <= 0)
        {
            // 30% chance de voltar ao idle
            if (_random.NextDouble() < 0.3)
            {
                brain.CurrentState = AIState.Idle;
                brain.StateTimer = RandomRange(behaviour.IdleDurationMin, behaviour.IdleDurationMax);
                dir.X = 0;
                dir.Y = 0;
                speed.Value = 0f;
                navigation.Destination = null;
                navigation.IsPathPending = false;
                return;
            }

            var radius = (int)System.Math.Ceiling(behaviour.PatrolRadius);
            var dx = _random.Next(-radius, radius + 1);
            var dy = _random.Next(-radius, radius + 1);
            var patrolTarget = new Position { X = spawn.X + dx, Y = spawn.Y + dy, Z = spawn.Z };

            if (navigation.Destination is null || !navigation.Destination.Value.Equals(patrolTarget))
            {
                navigation.Destination = patrolTarget;
                navigation.IsPathPending = true;
            }

            // NavigationSystem will compute direction.
            dir.X = 0;
            dir.Y = 0;
            brain.StateTimer = RandomRange(1.5f, 3.0f);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleChase(
        ref Brain brain, 
        ref NavigationAgent navigation,
        ref Direction dir, 
        ref Speed speed,
        in AIBehaviour behaviour, 
        in Position pos, 
        in Walkable walk,
        in SpawnPoint spawn)
    {
        // Alvo inválido - volta para casa
        if (brain.CurrentTarget == Entity.Null || ! World.IsAlive(brain. CurrentTarget))
        {
            brain.CurrentState = AIState.ReturnHome;
            brain.CurrentTarget = Entity.Null;
            return;
        }
        
        // Verifica se alvo está morto
        if (World.Has<Dead>(brain. CurrentTarget))
        {
            brain.CurrentState = AIState.ReturnHome;
            brain.CurrentTarget = Entity.Null;
            return;
        }
        
        var targetPos = World.Get<Position>(brain.CurrentTarget);
        
        float distToTarget = Distance(pos, targetPos);
        float distToSpawn = Distance(pos, new Position { X = spawn.X, Y = spawn.Y, Z = spawn.Z });

        // Regra de Leash: Se afastou demais do spawn, desiste
        if (distToSpawn > behaviour.LeashRange)
        {
            brain. CurrentState = AIState.ReturnHome;
            brain.CurrentTarget = Entity.Null;
            logger?.LogDebug("[AI] Entity lost target due to leash range");
            return;
        }

        // Se perdeu linha de visão (alvo muito longe), desiste
        if (distToTarget > behaviour.VisionRange * 1.5f)
        {
            brain.CurrentState = AIState.ReturnHome;
            brain.CurrentTarget = Entity.Null;
            logger?.LogDebug("[AI] Entity lost target - out of vision range");
            return;
        }

        // Se está no alcance de ataque, entra em combate
        if (distToTarget <= behaviour.AttackRange)
        {
            brain.CurrentState = AIState.Combat;
            speed.Value = 0f;
            dir.X = 0;
            dir.Y = 0;
            navigation.Destination = null;
            navigation.IsPathPending = false;
            return;
        }

        // Persegue
        speed.Value = ComputeCellsPerSecond(in walk, isSprinting: true);
        navigation.StoppingDistance = behaviour.AttackRange;
        if (navigation.Destination is null || !navigation.Destination.Value.Equals(targetPos))
        {
            navigation.Destination = targetPos;
            navigation.IsPathPending = true;
        }
        dir.X = 0;
        dir.Y = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleCombat(
        in Entity entity,
        ref Brain brain,
        ref NavigationAgent navigation,
        ref Direction dir,
        ref Speed speed,
        ref CombatState combatState,
        in AIBehaviour behaviour,
        in Position pos,
        in SpawnPoint spawn,
        in CombatStats combatStats,
        float deltaTime)
    {
        // Para o movimento durante o combate
        dir.X = 0;
        dir.Y = 0;
        speed.Value = 0f;
        navigation.Destination = null;
        navigation.IsPathPending = false;

        // Valida alvo
        if (brain.CurrentTarget == Entity. Null || !World.IsAlive(brain.CurrentTarget))
        {
            brain.CurrentState = AIState.ReturnHome;
            brain.CurrentTarget = Entity.Null;
            return;
        }

        // Verifica se alvo morreu
        if (World.Has<Dead>(brain.CurrentTarget))
        {
            brain.CurrentState = AIState.ReturnHome;
            brain.CurrentTarget = Entity.Null;
            logger?.LogDebug("[AI] Target died, returning home");
            return;
        }

        var targetPos = World.Get<Position>(brain.CurrentTarget);
        float distToTarget = Distance(pos, targetPos);
        float distToSpawn = Distance(pos, new Position { X = spawn.X, Y = spawn. Y, Z = spawn.Z });

        // Se saiu do leash range, volta para casa
        if (distToSpawn > behaviour.LeashRange)
        {
            brain. CurrentState = AIState.ReturnHome;
            brain.CurrentTarget = Entity.Null;
            return;
        }

        // Se alvo saiu do alcance de ataque, persegue novamente
        if (distToTarget > behaviour.AttackRange)
        {
            brain.CurrentState = AIState.Chase;
            return;
        }

        // Atualiza direção para olhar para o alvo
        SetDirectionTowards(ref dir, pos, targetPos);

        // Atualiza cooldown
        if (combatState.InCooldown)
        {
            combatState.CooldownTimer -= deltaTime;
            if (combatState.CooldownTimer <= 0f)
            {
                combatState.InCooldown = false;
                combatState.CooldownTimer = 0f;
            }
            return;
        }

        // Pode atacar - cria comando de ataque se não existir
        if (! World.Has<AttackCommand>(entity))
        {
            // Determina estilo de ataque baseado no comportamento/range
            var attackStyle = AttackHelpers.DetermineAttackStyle(combatStats);
            
            World.Add(entity, new AttackCommand
            {
                Target = brain.CurrentTarget,
                TargetPosition = targetPos,
                Style = attackStyle,
                ConjureDuration = AttackHelpers.GetBaseConjureTime(attackStyle),
            });

            // Inicia cooldown
            combatState.InCooldown = true;
            combatState.CooldownTimer = 1f / combatStats.AttackSpeed;
            combatState.LastAttackTime = brain.StateTimer;

            logger?.LogDebug("[AI] Entity {Entity} attacking {Target} with {Style}", 
                entity, brain.CurrentTarget, attackStyle);
        }
    }

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    private void HandleReturnHome(
        ref Brain brain, 
        ref NavigationAgent navigation,
        ref Direction dir, 
        ref Speed speed,
        in AIBehaviour behaviour, 
        in Position pos, 
        in Walkable walkable,
        in SpawnPoint spawn)
    {
        var spawnPos = new Position { X = spawn.X, Y = spawn.Y, Z = spawn.Z };
        float dist = Distance(pos, spawnPos);

        if (dist <= 1.0f) // Chegou em casa
        {
            brain. CurrentState = AIState. Idle;
            brain.StateTimer = RandomRange(behaviour.IdleDurationMin, behaviour.IdleDurationMax);
            dir.X = 0;
            dir.Y = 0;
            speed.Value = 0f;
            navigation.Destination = null;
            navigation.IsPathPending = false;
            return;
        }

        speed.Value = ComputeCellsPerSecond(in walkable, isSprinting: true);
        navigation.StoppingDistance = 1.0f;
        if (navigation.Destination is null || !navigation.Destination.Value.Equals(spawnPos))
        {
            navigation.Destination = spawnPos;
            navigation.IsPathPending = true;
        }
        dir.X = 0;
        dir.Y = 0;
    }

    private void ScanForTargets(
        ref Brain brain, 
        in AIBehaviour behaviour, 
        in Position pos, 
        in SpawnPoint spawn,
        in MapId mapId)
    {
        // Passivos nunca atacam
        if (behaviour.Type == BehaviorType.Passive)
            return;

        // Neutros só atacam se atacados (implementar via evento de dano)
        if (behaviour. Type == BehaviorType. Neutral)
            return;

        // Verifica se o mapa existe
        if (!mapIndex.HasMap(mapId. Value))
            return;

        var spatial = mapIndex.GetMapSpatial(mapId.Value);
        
        // Busca entidades próximas dentro do range de visão
        Entity closestTarget = Entity.Null;
        float closestDistance = float.MaxValue;

        // Itera sobre posições ao redor (grid-based search)
        int searchRadius = (int)Math. Ceiling(behaviour.VisionRange);
        
        for (int dx = -searchRadius; dx <= searchRadius; dx++)
        {
            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                var checkPos = new Position 
                { 
                    X = pos.X + dx, 
                    Y = pos.Y + dy, 
                    Z = pos.Z 
                };

                // Tenta obter entidade na posição
                if (! spatial.TryGetFirstAt(checkPos, out var candidate))
                    continue;

                // Ignora a si mesmo
                if (candidate == Entity.Null || !World.IsAlive(candidate))
                    continue;

                // Só ataca jogadores (entidades com PlayerControlled)
                if (!World. Has<PlayerControlled>(candidate))
                    continue;

                // Ignora entidades mortas
                if (World.Has<Dead>(candidate))
                    continue;

                // Ignora invulneráveis
                if (World. Has<Invulnerable>(candidate))
                    continue;

                // Calcula distância
                if (! World.TryGet<Position>(candidate, out var candidatePos))
                    continue;

                float distance = Distance(pos, candidatePos);

                // Verifica se está dentro do range de visão
                if (distance > behaviour.VisionRange)
                    continue;

                // Verifica leash range (não persegue se for sair muito do spawn)
                var spawnPos = new Position { X = spawn. X, Y = spawn.Y, Z = spawn.Z };
                float distFromSpawn = Distance(candidatePos, spawnPos);
                if (distFromSpawn > behaviour.LeashRange)
                    continue;

                // Encontra o mais próximo
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = candidate;
                }
            }
        }

        // Se encontrou um alvo válido, começa a perseguir
        if (closestTarget != Entity. Null)
        {
            brain.CurrentTarget = closestTarget;
            brain.CurrentState = AIState.Chase;
            logger?.LogDebug("[AI] Found target {Target} at distance {Distance}", 
                closestTarget, closestDistance);
        }
    }

    // --- Helpers Utilitários ---

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    private static void SetDirectionTowards(ref Direction dir, Position from, Position to)
    {
        int dx = to.X - from. X;
        int dy = to.Y - from.Y;

        // Normaliza simples para grid (8 direções)
        dir.X = (sbyte)Math.Sign(dx);
        dir.Y = (sbyte)Math.Sign(dy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Distance(Position a, Position b)
    {
        float dx = a.X - b. X;
        float dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float RandomRange(float min, float max)
    {
        return (float)(_random.NextDouble() * (max - min) + min);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ComputeCellsPerSecond(in Walkable walkable, bool isSprinting = false)
    {
        float speed = walkable.BaseSpeed + walkable.CurrentModifier;
        if (isSprinting)
            speed *= 1.5f;
        return speed;
    }
}