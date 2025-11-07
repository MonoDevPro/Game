using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável pela lógica de combate: ataque, dano, morte.
/// Processa ataques, aplica dano e gerencia transições para estado Dead.
/// </summary>
public sealed partial class CombatSystem(World world, IMapService mapService) 
    : GameSystem(world)
{
    private const float BaseAttackAnimationDuration = 1f;
    
    /// <summary>
    /// Processa ataques do player baseado no input do jogador.
    /// </summary>
    [Query]
    [All<PlayerInput, PlayerControlled, Position, CombatState, Attackable, AttackPower>]
    [None<Dead>]
    private void ProcessPlayerAttack(
        in Entity e,
        ref PlayerInput input,
        ref CombatState combat,
        in Facing facing,
        in Position position,
        in MapId mapId,
        ref DirtyFlags dirty,
        [Data] float deltaTime)
    {
        // Reduz cooldown
        if (combat.LastAttackTime > 0)
            combat.LastAttackTime -= deltaTime;

        // Se o player não ativou a flag de ataque, sair
        if ((input.Flags & InputFlags.Attack) == 0)
            return;

        // Busca o alvo mais próximo (ou alvo pré-selecionado)
        if (TryFindNearestTarget(mapId, position, facing, out Entity target))
        {
            if (CombatLogic.TryAttack(World, e, target))
            {
                // Ataque bem-sucedido!
                // Adiciona componente de animação de ataque
                int damage = CombatLogic.CalculateDamage(
                    World.Get<AttackPower>(e),
                    World.Get<Defense>(target));

                var attackAnim = new AttackState
                {
                    DefenderNetworkId = World.Get<NetworkId>(target).Value,
                    RemainingDuration = BaseAttackAnimationDuration,
                    Damage = damage,
                    WasHit = true,
                    AnimationType = AttackAnimationType.Basic
                };

                World.Add(e, attackAnim);
                dirty.MarkDirty(DirtyComponentType.CombatState);
            }
        }
        else
        {
            var attackAnim = new AttackState
            {
                DefenderNetworkId = 0,
                RemainingDuration = BaseAttackAnimationDuration,
                Damage = 0,
                WasHit = false,
                AnimationType = AttackAnimationType.Basic
            };

            World.Add(e, attackAnim);
            dirty.MarkDirty(DirtyComponentType.CombatState);
        }

        // Limpa a flag de ataque após o processamento
        input.Flags &= ~InputFlags.Attack;
    }

    /// <summary>
    /// Encontra o alvo mais próximo para ataque.
    /// </summary>
    private bool TryFindNearestTarget(in MapId playerMap, in Position playerPos, in Facing playerFacing,
        out Entity nearestTarget)
    {
        var spatial = mapService.GetMapSpatial(playerMap.Value);
        
        nearestTarget = Entity.Null;
        var targetPosition = new Position(
            playerPos.X + playerFacing.DirectionX,
            playerPos.Y + playerFacing.DirectionY,
            playerPos.Z);
        
        if (!spatial.TryGetFirstAt(targetPosition, out nearestTarget))
            return false;
        
        if (World.Has<Dead>(nearestTarget) || !World.Has<Attackable>(nearestTarget))
            return false;
        
        return true;
    }

    [Query]
    [All<Health, CombatState>]
    [None<Dead>]
    private void ProcessTakeDamage(in Entity e, ref Health health, ref CombatState combat, [Data] float deltaTime)
    {
        if (health.Current <= 0)
        {
            health.Current = 0;
            World.Add<Dead>(e);
        }
    }
}