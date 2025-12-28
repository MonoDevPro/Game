using Game.Domain.Enums;
using Game.Domain.ValueObjects.Attributes;

namespace Game.Domain.ValueObjects.Character;

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