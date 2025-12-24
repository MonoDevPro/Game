using Game.Domain.ValueObjects.Attributes;
using Game.Domain.Attributes.Vocation;
using Game.Domain.Attributes.Vocation.ValueObjects;
using Game.Domain.Enums;
using Game.Domain.Player;

namespace Game.Domain.Commons.Extensions;

/// <summary>
/// Extensões úteis para VocationType.
/// </summary>
public static class VocationExtensions
{
    public static VocationInfo GetInfo(this VocationType type) => VocationRegistry.Get(type);
    
    public static bool IsBaseVocation(this VocationType type) => 
        type.GetInfo().Tier == VocationTier.Base;
    
    public static bool IsPromoted(this VocationType type) => 
        type.GetInfo().Tier >= VocationTier.Promoted;
    
    public static VocationType? GetBaseVocation(this VocationType type) => 
        type.GetInfo().BaseVocation;
    
    public static BaseStats GetBaseStats(this VocationType type) => 
        type.GetInfo().BaseStats;
    
    public static StatsModifier GetGrowthModifiers(this VocationType type) => 
        type.GetInfo().GrowthModifiers;

    public static bool CanPromote(this VocationType current, VocationType target, int level) =>
        VocationRegistry.Get(current).CanPromoteTo(target, level);
}