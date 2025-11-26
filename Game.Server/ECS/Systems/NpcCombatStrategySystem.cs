using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Game.Server.Npc;
using System;

namespace Game.Server.ECS.Systems;

public sealed partial class NpcCombatStrategySystem(World world) : GameSystem(world)
{
    [Query]
    [All<NpcBrain, CombatStats, CombatState, NavigationAgent, Position>]
    public void UpdateCombatStrategy(
        in Entity entity,
        ref NpcBrain brain, 
        ref CombatStats stats, 
        ref CombatState state, 
        ref NavigationAgent nav, 
        in Position pos,
        [Data] float deltaTime)
    {
        // Reduz cooldowns
        state.AttackCooldownTimer = MathF.Max(0f, state.AttackCooldownTimer - deltaTime);

        // Se não temos alvo, não há estratégia de combate
        if (brain.CurrentState != NpcState.Combat || !World.IsAlive(brain.CurrentTarget)) 
            return;

        // Calcular distâncias
        if (!World.TryGet(brain.CurrentTarget, out Position targetPos)) return; // Alvo sumiu?
        
        float distSq = DistanceSquared(pos, targetPos);
        float rangeSq = stats.AttackRange * stats.AttackRange;

        // Lógica de Kiting (Exemplo para Arqueiros/Magos)
        // Se sou Ranged E o alvo está muito perto E meu ataque está em cooldown: FUGIR
        bool needsToKite = stats.AttackRange > 2f && distSq < (rangeSq * 0.5f) && state.AttackCooldownTimer > 0.5f;

        if (needsToKite)
        {
            // Define destino para longe do alvo
            nav.Destination = CalculateFleePosition(pos, targetPos);
            nav.StoppingDistance = 0f;
        }
        else if (distSq <= rangeSq)
        {
            // ESTOU NO RANGE
            nav.Destination = null; // Para de andar para atacar (ou continua se for mobile attack)
            
            // Se cooldown zerou, emite o COMANDO de ataque
            if (state.AttackCooldownTimer <= 0 && !World.Has<AttackCommand>(entity))
            {
                // Adiciona o componente de comando. O sistema de resolução vai pegar isso.
                World.Add(entity, new AttackCommand { Target = brain.CurrentTarget, IsReady = true });
                
                // Reseta cooldown baseada na velocidade de ataque
                state.AttackCooldownTimer = 1f / stats.AttackSpeed; 
            }
        }
        else
        {
            // FORA DO RANGE: Perseguir
            nav.Destination = targetPos;
            nav.StoppingDistance = stats.AttackRange * 0.8f; // Para um pouco antes do range máx
        }
    }

    private float DistanceSquared(in Position a, in Position b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private Position CalculateFleePosition(in Position myPos, in Position targetPos)
    {
        float dx = myPos.X - targetPos.X;
        float dy = myPos.Y - targetPos.Y;
        
        float len = MathF.Sqrt(dx*dx + dy*dy);
        if (len < 0.001f) return new Position { X = myPos.X + 1, Y = myPos.Y }; 
        
        float fleeDist = 5f;
        return new Position 
        { 
            X = (int)(myPos.X + (dx / len) * fleeDist), 
            Y = (int)(myPos.Y + (dy / len) * fleeDist) 
        };
    }
}
