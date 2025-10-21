using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Utils;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável pela IA de NPCs e entidades controladas por IA.
/// Processa movimento, decisões de combate e comportamento de NPCs.
/// </summary>
public sealed partial class AISystem(World world) : GameSystem(world)
{
    private readonly Random _random = new();

    [Query]
    [All<AIControlled, Position, Velocity, Facing>]
    private void ProcessAIMovement(in Entity e, ref Position pos, ref Velocity vel, ref Facing facing, [Data] float deltaTime)
    {
        // Lógica simples de IA: andar aleatoriamente
        // Pode ser expandido para pathfinding, comportamentos, etc
        
        if (_random.Next(0, 100) < 20) // 20% de chance de mudar direção a cada frame
        {
            int randomDir = _random.Next(0, 4);
            (vel.DirectionX, vel.DirectionY) = randomDir switch
            {
                0 => (1, 0),   // Direita
                1 => (-1, 0),  // Esquerda
                2 => (0, 1),   // Cima
                3 => (0, -1),  // Baixo
                _ => (0, 0)    // Parado
            };

            facing.DirectionX = vel.DirectionX;
            facing.DirectionY = vel.DirectionY;
            vel.Speed = 3f; // Velocidade padrão de NPCs
            
            World.MarkNetworkDirty(e, SyncFlags.Movement | SyncFlags.Facing);
        }
    }

    [Query]
    [All<AIControlled, CombatState, Position, Health>]
    private void ProcessAICombat(in Entity e, ref CombatState combat, in Position pos, in Health health, [Data] float deltaTime)
    {
        // Lógica de IA para combate
        if (health.Current <= 0)
            return;

        // Se em combate, pode tentar atacar
        if (combat.InCombat)
        {
            // Lógica de ataque: verificar cooldown, alcance, etc
            if (combat.LastAttackTime <= 0)
            {
                // IA decide atacar
                combat.LastAttackTime = 1.5f; // Cooldown de 1.5 segundos
                World.MarkNetworkDirty(e, SyncFlags.Movement);
            }
        }
    }

    /// <summary>
    /// Faz uma entidade IA atacar um alvo.
    /// </summary>
    public bool TryAIAttack(Entity attacker, Entity target)
    {
        if (!World.IsAlive(attacker) || !World.IsAlive(target))
            return false;

        if (!World.TryGet(attacker, out CombatState combat))
            return false;

        if (combat.LastAttackTime > 0)
            return false; // Cooldown ainda ativo

        combat.LastAttackTime = 1.5f;
        combat.InCombat = true;
        combat.TargetNetworkId = World.TryGet(target, out NetworkId netId) ? (uint)netId.Value : 0;
        
        World.Set(attacker, combat);
        World.MarkNetworkDirty(attacker, SyncFlags.Movement);

        return true;
    }

    /// <summary>
    /// Para entidade IA de atacar.
    /// </summary>
    public void StopAICombat(Entity entity)
    {
        if (!World.IsAlive(entity) || !World.TryGet(entity, out CombatState combat))
            return;

        combat.InCombat = false;
        combat.TargetNetworkId = 0;
        World.Set(entity, combat);
    }
}
