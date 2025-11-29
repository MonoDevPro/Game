using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS;

/// <summary>
/// Defines predefined archetypes for common entity types in the game.
/// Using predefined archetypes improves entity creation performance by
/// pre-registering component combinations.
/// </summary>
public static class GameArchetypes
{
    /// <summary>
    /// Archetype for projectile entities.
    /// Includes transform and projectile-specific components.
    /// </summary>
    public static readonly ComponentType[] Projectile =
    [
        // Transform
        Component<Position>.ComponentType,
        Component<Floor>.ComponentType,
        Component<Direction>.ComponentType,
        Component<Speed>.ComponentType,
        
        // Projectile
        Component<Projectile>.ComponentType,
        Component<MapId>.ComponentType,
    ];
    
    /// <summary>
    /// Minimal archetype for visual-only entities on the client.
    /// Does not include AI or input components.
    /// </summary>
    public static readonly ComponentType[] VisualEntity =
    [
        Component<NetworkId>.ComponentType,
        Component<Position>.ComponentType,
        Component<Floor>.ComponentType,
        Component<Direction>.ComponentType,
        Component<Speed>.ComponentType,
        Component<Health>.ComponentType,
        Component<Mana>.ComponentType,
    ];
}
