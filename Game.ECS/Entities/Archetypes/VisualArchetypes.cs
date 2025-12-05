using Arch.Core;
using Game.ECS.Schema.Components;

namespace Game.ECS.Schema.Archetypes;

/// <summary>
/// Defines predefined archetypes for common entity types in the game.
/// Using predefined archetypes improves entity creation performance by
/// pre-registering component combinations.
/// </summary>
public static class VisualArchetypes
{
    /// <summary>
    /// Minimal archetype for visual-only entities on the client.
    /// Does not include AI or input components.
    /// </summary>
    public static readonly ComponentType[] VisualEntity =
    [
        Component<NetworkId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<Position>.ComponentType,
        Component<Floor>.ComponentType,
        Component<Direction>.ComponentType,
        Component<Speed>.ComponentType,
        Component<Health>.ComponentType,
        Component<Mana>.ComponentType,
    ];
}
