using Game.Domain.Commons.Enums;
using Game.Domain.Commons.ValueObjects.Attributes;
using Game.Domain.Vocations.ValueObjects;

namespace Game.Domain.Vocations;

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
}