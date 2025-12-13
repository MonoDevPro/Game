using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Archetypes;

public static class ProjectileArchetypes
{
    /// <summary>
    /// Archetype for projectile entities.
    /// Includes transform and projectile-specific components.
    /// </summary>
    public static readonly ComponentType[] Projectile =
    [
        // Transform
        Component<Position>.ComponentType,
        Component<Direction>.ComponentType,
        Component<Speed>.ComponentType,
        
        // Projectile
        Component<Projectile>.ComponentType,
        Component<MapId>.ComponentType,
    ];
}