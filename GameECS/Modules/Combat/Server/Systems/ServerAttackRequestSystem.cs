using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameECS.Modules.Combat.Server.Components;
using GameECS.Modules.Combat.Shared.Components;
using GameECS.Modules.Combat.Shared.Core;
using GameECS.Modules.Combat.Shared.Data;
using GameECS.Modules.Combat.Shared.Systems;

namespace GameECS.Modules.Combat.Server.Systems;

/// <summary>
/// Sistema que processa requisições de ataque no servidor.
/// </summary>
public sealed partial class ServerAttackRequestSystem : BaseSystem<World, long>
{
    private readonly CombatService _combatService;
    private readonly int _maxRequestsPerTick;
    private readonly Action<DamageNetworkMessage>? _onDamageDealt;
    private readonly Action<DeathNetworkMessage>? _onEntityDeath;
    private int _processedThisTick;

    public ServerAttackRequestSystem(
        World world, 
        CombatService combatService,
        int maxRequestsPerTick = 100,
        Action<DamageNetworkMessage>? onDamageDealt = null,
        Action<DeathNetworkMessage>? onEntityDeath = null) : base(world)
    {
        _combatService = combatService;
        _maxRequestsPerTick = maxRequestsPerTick;
        _onDamageDealt = onDamageDealt;
        _onEntityDeath = onEntityDeath;
    }

    public override void BeforeUpdate(in long t)
    {
        _processedThisTick = 0;
    }

    [Query]
    [All<AttackRequest, CombatStats, Vocation, Health, CanAttack>]
    private void ProcessAttackRequestQuery(
        [Data] in long serverTick,
        Entity entity,
        ref AttackRequest request)
    {
        if (_processedThisTick >= _maxRequestsPerTick) return;
        
        ProcessAttackRequest(entity, ref request, serverTick);
        _processedThisTick++;
    }

    private void ProcessAttackRequest(Entity attacker, ref AttackRequest request, long serverTick)
    {
        // Remove a requisição após processar
        World.Remove<AttackRequest>(attacker);

        // Tenta encontrar o alvo
        if (!TryGetEntityById(request.TargetEntityId, out var target))
        {
            RecordAttackResult(attacker, AttackResult.NoTarget, 0, request.TargetEntityId, false, serverTick);
            return;
        }

        // Obtém posições (integração com módulo de navegação seria aqui)
        // Por enquanto, assumimos que as posições são válidas
        var (attackerX, attackerY) = GetEntityPosition(attacker);
        var (targetX, targetY) = GetEntityPosition(target);

        // Executa o ataque
        var (result, damageInfo) = _combatService.ExecuteBasicAttack(
            World, attacker, target,
            attackerX, attackerY,
            targetX, targetY,
            serverTick);

        // Registra resultado
        RecordAttackResult(attacker, result, damageInfo?.FinalDamage ?? 0, 
            request.TargetEntityId, damageInfo?.IsCritical ?? false, serverTick);

        // Notifica via callback
        if (damageInfo.HasValue)
        {
            _onDamageDealt?.Invoke(new DamageNetworkMessage
            {
                AttackerId = attacker.Id,
                TargetId = target.Id,
                Damage = damageInfo.Value.FinalDamage,
                Type = damageInfo.Value.Type,
                IsCritical = damageInfo.Value.IsCritical,
                Result = result,
                ServerTick = serverTick
            });

            // Verifica morte
            if (World.Has<Dead>(target))
            {
                _onEntityDeath?.Invoke(new DeathNetworkMessage
                {
                    EntityId = target.Id,
                    KillerId = attacker.Id,
                    ServerTick = serverTick
                });
            }

            // Atualiza estatísticas
            if (World.Has<CombatStatistics>(attacker))
            {
                ref var stats = ref World.Get<CombatStatistics>(attacker);
                stats.RecordDamageDealt(damageInfo.Value.FinalDamage, damageInfo.Value.IsCritical);
                
                if (World.Has<Dead>(target))
                    stats.RecordKill();
            }

            if (World.Has<CombatStatistics>(target))
            {
                ref var stats = ref World.Get<CombatStatistics>(target);
                stats.RecordDamageReceived(damageInfo.Value.FinalDamage);
                
                if (World.Has<Dead>(target))
                    stats.RecordDeath();
            }
        }
    }

    private void RecordAttackResult(Entity attacker, AttackResult result, int damage, 
        int targetId, bool wasCritical, long tick)
    {
        if (World.Has<LastAttackResult>(attacker))
        {
            ref var lastResult = ref World.Get<LastAttackResult>(attacker);
            lastResult.Result = result;
            lastResult.DamageDealt = damage;
            lastResult.TargetEntityId = targetId;
            lastResult.WasCritical = wasCritical;
            lastResult.Tick = tick;
        }
        else
        {
            World.Add(attacker, new LastAttackResult
            {
                Result = result,
                DamageDealt = damage,
                TargetEntityId = targetId,
                WasCritical = wasCritical,
                Tick = tick
            });
        }
    }

    private bool TryGetEntityById(int entityId, out Entity entity)
    {
        // Busca entidade pelo ID
        var query = new QueryDescription().WithAll<CombatEntity, Health>();
        Entity foundEntity = default;
        
        bool found = false;
        World.Query(in query, (Entity e) =>
        {
            if (e.Id == entityId)
            {
                foundEntity = e;
                found = true;
            }
        });

        entity = foundEntity;
        return found;
    }

    private (int x, int y) GetEntityPosition(Entity entity)
    {
        // TODO: Integrar com módulo de navegação para obter GridPosition
        // Por enquanto retorna posição padrão
        return (0, 0);
    }
}
