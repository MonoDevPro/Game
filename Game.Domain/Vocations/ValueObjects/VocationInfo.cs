using Game.Domain.Commons.Enums;
using Game.Domain.Commons.ValueObjects.Attributes;

namespace Game.Domain.Vocations.ValueObjects;

/// <summary>
/// Metadados completos de uma vocação.
/// </summary>
public sealed record VocationInfo(
    string Name,
    string Description,
    VocationType Type,
    VocationArchetype Archetype,
    BaseStats BaseStats,
    BaseStats GrowthModifiers);