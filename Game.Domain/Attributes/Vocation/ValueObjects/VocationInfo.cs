using Game.Domain.ValueObjects.Attributes;
using Game.Domain.Enums;
using Game.Domain.Entities;
using Game.Domain.DomainServices;

namespace Game.Domain.Attributes.Vocation.ValueObjects;

/// <summary>
/// Metadados completos de uma vocação.
/// </summary>
public sealed record VocationInfo(
    VocationType Type,
    string Name,
    string Description,
    VocationTier Tier,
    VocationArchetype Archetype,
    VocationType? BaseVocation,
    int PromotionLevel,
    BaseStats BaseStats,
    StatsModifier GrowthModifiers,
    VocationCombatProfile CombatProfile)
{
    /// <summary>
    /// Vocações para as quais esta pode evoluir.
    /// </summary>
    public VocationType[] Promotions { get; init; } = [];
    
    /// <summary>
    /// Verifica se pode promover para a vocação especificada.
    /// </summary>
    public bool CanPromoteTo(VocationType target, int currentLevel)
    {
        var targetInfo = VocationRegistry.Get(target);
        return targetInfo.BaseVocation == Type && currentLevel >= targetInfo.PromotionLevel;
    }
}