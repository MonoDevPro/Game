using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class CombatSystem(World world, IMapService mapService, ILogger<CombatSystem>? logger = null) 
    : GameSystem(world)
{
    private const float BaseAttackAnimationDuration = 1f;

    [Query]
    [All<PlayerInput, PlayerControlled, Position, CombatState, Attackable, AttackPower>]
    [None<AttackAction, Dead>]
    private void ProcessPlayerAttack(
        in Entity e,
        ref PlayerInput input,
        ref CombatState combat,
        in Facing facing,
        in Position position,
        in MapId mapId,
        [Data] float deltaTime)
    {
        // Reduz cooldown (helper mantém clamped)
        CombatLogic.ReduceCooldown(ref combat, deltaTime);
        
        // Se o player não ativou a flag de ataque, sair
        if ((input.Flags & InputFlags.Attack) == 0) return;
        
        // Se estiver em cooldown, sair
        if (!CombatLogic.CanAttack(in combat)) return;

        // Busca o alvo na célula à frente
        if (CombatLogic.TryFindNearestTarget(World, mapService, mapId, position, facing, out Entity target))
        {
            // Usa TryAttack com out damage para manter consistência com pacote
            if (CombatLogic.TryAttack(World, e, target, AttackType.Basic, out int damage))
            {
                // Marcar HP do alvo como dirty para enviar vitals
                if (World.Has<DirtyFlags>(target))
                    World.Get<DirtyFlags>(target).MarkDirty(DirtyComponentType.Health);
                
                var attackAction = new AttackAction
                {
                    DefenderNetworkId = World.Get<NetworkId>(target).Value,
                    Type = AttackType.Basic,
                    RemainingDuration = BaseAttackAnimationDuration,
                    WillHit = true,
                    Damage = damage,
                };
                var dirty = World.Get<DirtyFlags>(e);
                dirty.MarkDirty(DirtyComponentType.CombatState);
                World.Set(e, dirty);
                World.Add(e, attackAction);
            }
        }
        else
        {
            // Sem alvo à frente → animação de "whiff" e cooldown, para evitar spam
            if (World.TryGet(e, out Attackable atk))
            {
                combat.LastAttackTime = MathF.Max(combat.LastAttackTime,
                    CombatLogic.CalculateAttackCooldownSeconds(in atk, AttackType.Basic));
                combat.InCombat = true;
            }

            var attackAction = new AttackAction
            {
                DefenderNetworkId = 0,
                Type = AttackType.Basic,
                RemainingDuration = BaseAttackAnimationDuration,
                WillHit = false,
                Damage = 0,
            };
            var dirty = World.Get<DirtyFlags>(e);
            dirty.MarkDirty(DirtyComponentType.CombatState);
            World.Set(e, dirty);
            World.Add(e, attackAction);
        }
    }
}