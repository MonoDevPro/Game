using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Infrastructure.ArchECS.Commons;
using Game.Infrastructure.ArchECS.Services.Combat.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Microsoft.Extensions.Logging;

namespace Game.Infrastructure.ArchECS.Services.EntityRegistry.Systems;

/// <summary>
/// Sistema com queries otimizadas por Source Generator para gerenciar entidades por domínio.
/// Usa atributos [Query], [All], [Any], [None] para gerar código de alta performance.
/// </summary>
public partial class EntityRegistryQuerySystem(World world, ILogger? logger = null) : GameSystem(world, logger)
{
    private readonly CentralEntityRegistry _registry = world.GetEntityRegistry();

    #region Combat Domain Queries

    /// <summary>
    /// Query para entidades de combate ativas.
    /// Source Generator cria: QueryCombatEntitiesQuery(World)
    /// </summary>
    [Query]
    [All<CombatEntity, Vitals>]
    [None<Dead>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void QueryCombatEntities(in Entity entity, ref Vitals health)
    {
        // Processar entidades de combate vivas
        if (_registry.Contains(entity))
        {
            var metadata = _registry.GetMetadata(entity);
            
            if (metadata.Domain.HasFlag(EntityDomain.Combat))
            {
                LogTrace($"Combat Entity: {metadata} - HP: {health.CurrentHp}/{health.MaxHp}");
            }
        }
    }

    /// <summary>
    /// Query para entidades de combate em estado crítico (baixo HP).
    /// </summary>
    [Query]
    [All<CombatEntity, Vitals>]
    [None<Dead>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void QueryLowHealthCombatEntities(in Entity entity, ref Vitals health)
    {
        // Apenas entidades com menos de 30% HP
        if (health.CurrentHp <= health.MaxHp * 0.3f)
        {
            if (_registry.TryGetEntity(entity.Id, EntityDomain.Combat, out var foundEntity))
            {
                LogWarning($"Critical HP: Entity {entity} - {health.CurrentHp}/{health.MaxHp}");
            }
        }
    }

    /// <summary>
    /// Query para entidades mortas que precisam ser removidas do registry.
    /// </summary>
    [Query]
    [All<Dead>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CleanupDeadEntities(in Entity entity)
    {
        if (_registry.Contains(entity))
        {
            _registry.Unregister(entity);
            LogDebug($"Removed dead entity from registry: {entity}");
        }
    }

    #endregion

    #region Navigation Domain Queries

    /// <summary>
    /// Query para entidades de navegação em movimento.
    /// </summary>
    [Query]
    [All<NavAgent, NavMovementState, NavIsMoving>]
    [None<NavWaitingToMove>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void QueryMovingNavigationEntities(in Entity entity, ref NavMovementState movement)
    {
        if (_registry.Contains(entity))
        {
            var metadata = _registry.GetMetadata(entity);
            
            if (metadata.Domain.HasFlag(EntityDomain.Navigation))
            {
                LogTrace($"Moving: {metadata} - Target: {movement.TargetCell}");
            }
        }
    }

    /// <summary>
    /// Query para entidades bloqueadas aguardando movimento.
    /// </summary>
    [Query]
    [All<NavAgent, NavWaitingToMove>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void QueryBlockedNavigationEntities(in Entity entity, ref NavWaitingToMove waiting)
    {
        LogWarning($"Entity {entity} blocked by entity ID: {waiting.BlockedByEntityId}");
    }

    #endregion

    #region Multi-Domain Queries

    /// <summary>
    /// Query para entidades que participam de múltiplos domínios (Combat + Navigation).
    /// Ideal para NPCs e jogadores.
    /// </summary>
    [Query]
    [All<CombatEntity, NavAgent, Vitals>]
    [None<Dead>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void QueryMultiDomainEntities(in Entity entity, ref Vitals health, ref NavMovementState movement)
    {
        if (_registry.Contains(entity))
        {
            var metadata = _registry.GetMetadata(entity);
            
            // Entidade participa de Combat E Navigation
            if (metadata.Domain.HasFlag(EntityDomain.Combat | EntityDomain.Navigation))
            {
                LogDebug($"Multi-domain: {metadata} - HP: {health.CurrentHp}, Moving: {movement.IsMoving}");
            }
        }
    }

    /// <summary>
    /// Query para entidades com AI, Combat e Navigation (NPCs completos).
    /// </summary>
    [Query]
    [All<AIComponent, CombatEntity, NavAgent>]
    [None<Dead, Disabled>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void QueryFullNPCEntities(in Entity entity)
    {
        if (_registry.Contains(entity))
        {
            var metadata = _registry.GetMetadata(entity);
            LogInformation($"Full NPC: {metadata} - Domains: {metadata.Domain}");
        }
    }

    #endregion

    #region Conditional Domain Queries (Any)

    /// <summary>
    /// Query para entidades que estão em Combat OU Navigation (pelo menos um).
    /// </summary>
    [Query]
    [Any<CombatEntity, NavAgent>]
    [None<Dead>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void QueryCombatOrNavigationEntities(in Entity entity)
    {
        if (_registry.Contains(entity))
        {
            var metadata = _registry.GetMetadata(entity);
            
            var domains = new List<string>();
            if (metadata.Domain.HasFlag(EntityDomain.Combat)) domains.Add("Combat");
            if (metadata.Domain.HasFlag(EntityDomain.Navigation)) domains.Add("Navigation");
            
            LogTrace($"Entity {metadata} in domains: {string.Join(", ", domains)}");
        }
    }

    #endregion

    #region Statistics & Maintenance

    /// <summary>
    /// Query para validar consistência entre registry e world.
    /// Executado periodicamente.
    /// </summary>
    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ValidateRegistryConsistency(in Entity entity)
    {
        // Verificar se entidade existe no world mas não no registry
        if (!_registry.Contains(entity))
        {
            LogWarning($"Entity {entity} exists in World but not in Registry!");
        }
    }

    #endregion

    #region System Lifecycle

    public override void Initialize()
    {
        base.Initialize();
        LogInformation("EntityRegistryQuerySystem initialized");
    }

    public override void Update(in long deltaTime)
    {
        // Source Generator cria métodos *Query automaticamente:
        // - QueryCombatEntitiesQuery(World)
        // - QueryLowHealthCombatEntitiesQuery(World)
        // - QueryMovingNavigationEntitiesQuery(World)
        // etc...
        
        // Executar queries de manutenção
        CleanupDeadEntitiesQuery(World);
        ValidateRegistryConsistencyQuery(World);
    }

    public override void Dispose()
    {
        LogInformation("EntityRegistryQuerySystem disposed");
        base.Dispose();
    }

    #endregion
}