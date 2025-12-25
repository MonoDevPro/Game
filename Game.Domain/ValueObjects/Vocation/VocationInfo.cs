using Game.Domain.Enums;
using Game.Domain.ValueObjects.Attributes;

namespace Game.Domain.ValueObjects.Vocation;

/// <summary>
/// Metadados completos de uma vocação.
/// </summary>
public sealed record VocationInfo(
    VocationType Type,
    string Name,
    string Description,
    VocationArchetype Archetype,
    BaseStats BaseStats,
    BaseStats GrowthModifiers,
    VocationCombatProfile CombatProfile);
