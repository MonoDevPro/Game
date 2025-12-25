using Game.Domain.ValueObjects.Attributes;
using Game.Domain.Enums;
using Game.Domain.DomainServices;
using Game.Domain.ValueObjects.Vocation;

namespace Game.Domain.Extensions;

/// <summary>
/// Extensões úteis para VocationType.
/// </summary>
public static class VocationExtensions
{
    public static VocationInfo GetInfo(this VocationType type) => VocationRegistry.Get(type);
    
    public static BaseStats GetBaseStats(this VocationType type) => 
        type.GetInfo().BaseStats;
    
    public static BaseStats GetGrowthModifiers(this VocationType type) => 
        type.GetInfo().GrowthModifiers;
    
    public static VocationCombatProfile GetCombatProfile(this VocationType type) => 
        type.GetInfo().CombatProfile;
    
}