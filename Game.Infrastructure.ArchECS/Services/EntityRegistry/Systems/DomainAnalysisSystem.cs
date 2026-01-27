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
/// Sistema para análise e estatísticas de domínios.
/// Usa [Data] attribute para passar parâmetros customizados.
/// </summary>
public partial class DomainAnalysisSystem(World world, ILogger? logger = null) : GameSystem(world, logger)
{
    private readonly CentralEntityRegistry _registry = world.GetEntityRegistry();
    private DomainStatistics _statistics;

    #region Statistics Queries (Com [Data])

    /// <summary>
    /// Conta entidades de combate e acumula estatísticas.
    /// Usa [Data] para passar estrutura de stats.
    /// </summary>
    [Query]
    [All<CombatEntity, Vitals>]
    [None<Dead>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CountCombatEntities([Data] ref DomainStatistics stats, in Entity entity, in Vitals health)
    {
        stats.CombatCount++;
        stats.TotalHP += health.CurrentHp;
        stats.MaxHP += health.MaxHp;
        
        if (health.CurrentHp <= health.MaxHp * 0.3f)
        {
            stats.CriticalHealthCount++;
        }
    }

    /// <summary>
    /// Conta entidades de navegação.
    /// </summary>
    [Query]
    [All<NavAgent>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CountNavigationEntities([Data] ref DomainStatistics stats, in Entity entity)
    {
        stats.NavigationCount++;
        
        if (World.Has<NavIsMoving>(entity))
        {
            stats.MovingEntitiesCount++;
        }
    }

    /// <summary>
    /// Conta entidades multi-domínio.
    /// </summary>
    [Query]
    [All<CombatEntity, NavAgent>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CountMultiDomainEntities([Data] ref DomainStatistics stats, in Entity entity)
    {
        stats.MultiDomainCount++;
    }

    /// <summary>
    /// Identifica entidades órfãs (no World mas não no Registry).
    /// </summary>
    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DetectOrphanEntities([Data] ref DomainStatistics stats, in Entity entity)
    {
        if (!_registry.Contains(entity))
        {
            stats.OrphanEntitiesCount++;
        }
    }

    #endregion

    #region System Lifecycle

    public override void Update(in long deltaTime)
    {
        // Resetar stats
        _statistics.Reset();
        
        // Executar queries com [Data]
        CountCombatEntitiesQuery(World, ref _statistics);
        CountNavigationEntitiesQuery(World, ref _statistics);
        CountMultiDomainEntitiesQuery(World, ref _statistics);
        DetectOrphanEntitiesQuery(World, ref _statistics);

        // Log estatísticas a cada 60 frames
        if (deltaTime % 60 == 0)
        {
            LogInformation(_statistics.ToString());
        }
    }

    public DomainStatistics GetStatistics() => _statistics;

    #endregion
}

#region Statistics Structures

/// <summary>
/// Estatísticas de domínios.
/// Struct para performance (stack allocation).
/// </summary>
public struct DomainStatistics
{
    public int CombatCount;
    public int NavigationCount;
    public int MultiDomainCount;
    public int OrphanEntitiesCount;
    public int CriticalHealthCount;
    public int MovingEntitiesCount;
    
    public float TotalHP;
    public float MaxHP;

    public float AverageHP => CombatCount > 0 ? TotalHP / CombatCount : 0f;
    public float HPPercentage => MaxHP > 0 ? (TotalHP / MaxHP) * 100f : 0f;

    public void Reset()
    {
        CombatCount = 0;
        NavigationCount = 0;
        MultiDomainCount = 0;
        OrphanEntitiesCount = 0;
        CriticalHealthCount = 0;
        MovingEntitiesCount = 0;
        TotalHP = 0f;
        MaxHP = 0f;
    }

    public override string ToString()
    {
        return $"""
            Domain Statistics:
              Combat Entities: {CombatCount} (Critical: {CriticalHealthCount})
              Navigation Entities: {NavigationCount} (Moving: {MovingEntitiesCount})
              Multi-Domain Entities: {MultiDomainCount}
              Orphan Entities: {OrphanEntitiesCount}
              Average HP: {AverageHP:F1} ({HPPercentage:F1}%)
            """;
    }
}

/// <summary>
/// Métricas de saúde (para queries paralelas).
/// </summary>
public struct HealthMetrics
{
    public int Count;
    public int TotalHP;
    public int FullHealthCount;

    public float AverageHP => Count > 0 ? (float)TotalHP / Count : 0f;
    
    public void Add(in Vitals health)
    {
        System.Threading.Interlocked.Increment(ref Count);
    }
    
    
    public void Reset()
    {
        Count = 0;
        TotalHP = 0;
        FullHealthCount = 0;
    }
    
    
    
}

#endregion