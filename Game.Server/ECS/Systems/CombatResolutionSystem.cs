using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class CombatResolutionSystem(World world) : GameSystem(world)
{
    [Query]
    [All<AttackCommand, CombatStats, Position, MapId, Floor, Facing>]
    public void ResolveAttacks(
        Entity attacker, 
        ref AttackCommand cmd,
        ref CombatState state,
        in CombatStats stats,
        in Position pos,
        in MapId mapId,
        in Floor floor)
    {
        // Se já processou, ignora (esperando sync/limpeza)
        if (cmd.IsReady) return;
        
        // Se ainda está no "Wind-up" da animação, espera.
        // Isso permite que o ataque saia exatamente no frame certo da animação.
        if (state.IsCasting) return;
        
        state.AttackCooldownTimer = 1f / stats.AttackSpeed;

        // --- Execução do Ataque ---

        bool isRanged = stats.AttackRange > 2.0f;

        if (isRanged)
        {
            // Ranged aceita Target nulo (atira na direção/posição)
            SpawnProjectile(attacker, cmd.Target, cmd.TargetPosition, stats, pos, mapId, floor, cmd.Style);
        }
        else
        {
            // Melee exige alvo vivo
            if (World.IsAlive(cmd.Target))
            {
                 ApplyMeleeDamage(attacker, cmd.Target, stats);
            }
        }
        
        // Marca como processado. 
        // O componente deve ser removido por um sistema de limpeza ou sync posteriormente.
        cmd.IsReady = true; 
    }

    private void SpawnProjectile(
        Entity attacker, 
        Entity target,
        Position targetFixedPos, // Posição fixa do clique/mira
        in CombatStats stats, 
        in Position pos, 
        in MapId mapId, 
        in Floor floor, 
        AttackStyle style)
    {
        bool isMagical = style == AttackStyle.Magic;
        int damage = isMagical ? stats.MagicPower : stats.AttackPower;

        // Se temos um alvo travado (Homing), usamos a posição atual dele.
        // Se não (Skillshot), usamos a posição fixa salva no comando.
        Position actualTargetPos = targetFixedPos;
        if (World.IsAlive(target) && World.TryGet(target, out Position livePos))
        {
            actualTargetPos = livePos;
        }

        // Cria a entidade do projétil
        World.Create(
            new Projectile
            {
                Source = attacker,
                TargetPosition = actualTargetPos,
                CurrentX = pos.X,
                CurrentY = pos.Y,
                Speed = 8f,
                Damage = damage,
                IsMagical = isMagical,
                RemainingLifetime = 3f, // 3 segundos de vida máx
                HasHit = false
            },
            new MapId { Value = mapId.Value },
            new Floor { Level = floor.Level }
        );
    }

    private void ApplyMeleeDamage(Entity attacker, Entity target, in CombatStats stats)
    {
        // Validação de distância para evitar "Melee Sniper" (lag switch)
        if (World.TryGet(target, out Position tPos) && World.TryGet(attacker, out Position aPos))
        {
            if (PositionLogic.CalculateDistance(in aPos, in tPos) > stats.AttackRange)
                return; // Fora do alcance
        }

        int damageRoll = stats.AttackPower;
        // Variação de 10% no dano
        damageRoll = (int)(damageRoll * (0.9f + Random.Shared.NextSingle() * 0.2f));
        
        if (World.TryGet(target, out CombatStats targetStats))
            damageRoll = Math.Max(1, damageRoll - targetStats.Defense);

        // Aplica Dano Deferred
        DamageLogic.ApplyDeferredDamage(World, in target, damageRoll, isCritical: false, attacker: attacker);
    }
}