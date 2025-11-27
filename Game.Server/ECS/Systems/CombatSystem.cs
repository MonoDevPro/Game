using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Combat;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por atualizar o estado de combate dos jogadores.
/// Mantém um timer desde o último hit e limpa o flag de combate
/// após um período configurado, permitindo voltar a regenerar HP/MP.
/// </summary>
public sealed partial class CombatSystem(World world, IMapService mapService, ILogger<CombatSystem>? logger = null) : GameSystem(world)
{
    /// <summary>
    /// Processa ataques de jogadores (PlayerControlled).
    /// O tipo de ataque é determinado pela vocação do jogador.
    /// </summary>
    [Query]
    [All<PlayerControlled, Input, PlayerInfo, Position, MapId, Floor, Direction>]
    [None<Dead>]
    private void ProcessPlayerAttack(
        in Entity e,
        in PlayerInfo info,
        in Position pos,
        in MapId mapId,
        in Floor floor,
        in Direction direction,
        ref Input input,
        ref CombatState state,
        in CombatStats stats,
        ref DirtyFlags dirty,
        [Data] float deltaTime)
    {
        // 1. Atualização de Timers (Cooldown e Cast)
        if (state.AttackCooldownTimer > 0f)
        {
            state.AttackCooldownTimer -= deltaTime;
            state.InCooldown = state.AttackCooldownTimer > 0; // Atualiza flag baseado no timer
        }
        
        if (state.CastTimer > 0f)
        {
            state.CastTimer -= deltaTime;
            state.IsCasting = state.CastTimer > 0; // Atualiza flag baseado no timer
        }
        
        // 2. Validações de Entrada
        // Se não apertou botão, se está em cooldown, se está castando ou já tem comando pendente
        if ((input.Flags & InputFlags.BasicAttack) == 0) return;
        if (state.InCooldown || state.IsCasting || World.Has<AttackCommand>(e)) return;
        
        // 3. Preparação dos Dados do Ataque
        bool isRanged = stats.AttackRange > 2.0f; // Ou verificar Vocação
        Entity target = Entity.Null;
        Position targetPosition = default;
        
        // Lógica de Mira (Targeting)
        SpatialPosition lookAtPos = new(
            pos.X + direction.DirectionX, 
            pos.Y + direction.DirectionY, 
            floor.Level);
        
        // Tenta achar um alvo na frente (Raycast simples de 1 tile)
        // Para Ranged, isso poderia ser um Raycast mais longo no futuro
        var spatial = mapService.GetMapSpatial(mapId.Value);
        if (spatial.TryGetFirstAt(lookAtPos, out Entity foundEntity) && foundEntity != e)
        {
            target = foundEntity;
            if (World.TryGet(target, out Position tPos)) targetPosition = tPos;
        }
        else
        {
            // Se não achou entidade, o alvo é o "chão" na frente (para Skillshots/Projéteis)
            targetPosition = new Position { X = lookAtPos.X, Y = lookAtPos.Y };
        }
        
        // Se for Melee e não tem alvo válido, aborta (evita atacar o ar se não for desejado)
        if (!isRanged && target == Entity.Null) 
            return;
        
        // 4. Aplicação do Custo de Tempo (Cooldowns)
        // CastTimer: Tempo que fica "travado" realizando a animação
        // CooldownTimer: Tempo até poder iniciar o próximo ataque
        state.IsCasting = true;
        state.CastTimer = 1f / stats.AttackSpeed;;

        // 5. Emite o Comando
        dirty.MarkDirty(DirtyComponentType.Combat);
        
        World.Add(e, new AttackCommand 
        { 
            Target = target,
            TargetPosition = targetPosition, // Snapshot da posição
            Style = CombatHelper.GetAttackStyleForVocation(info.VocationId),
            IsReady = false 
        });
    }
}
