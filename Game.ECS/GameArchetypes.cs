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
    /// Full archetype for player entities with all required components.
    /// Includes identity, transform, combat, vitals, input, and synchronization components.
    /// </summary>
    public static readonly ComponentType[] Player =
    [
        // Identity
        Component<PlayerId>.ComponentType,
        Component<NetworkId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<NameHandle>.ComponentType,
        Component<GenderId>.ComponentType,
        Component<VocationId>.ComponentType,
        Component<PlayerInfo>.ComponentType,
        Component<PlayerControlled>.ComponentType,
        
        // Transform
        Component<Position>.ComponentType,
        Component<Floor>.ComponentType,
        Component<Direction>.ComponentType,
        Component<Velocity>.ComponentType,
        Component<Movement>.ComponentType,
        Component<Walkable>.ComponentType,
        
        // Combat
        Component<CombatStats>.ComponentType,
        Component<CombatState>.ComponentType,
        
        // Vitals
        Component<Health>.ComponentType,
        Component<Mana>.ComponentType,
        
        // Input & Sync
        Component<Input>.ComponentType,
        Component<DirtyFlags>.ComponentType,
    ];
    
    /// <summary>
    /// Full archetype for NPC entities with AI components.
    /// Includes identity, transform, combat, vitals, AI, and synchronization components.
    /// </summary>
    public static readonly ComponentType[] NPC =
    [
        // Identity
        Component<NetworkId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<NameHandle>.ComponentType,
        Component<GenderId>.ComponentType,
        Component<VocationId>.ComponentType,
        Component<AIControlled>.ComponentType,
        Component<NpcType>.ComponentType,
        
        // Transform
        Component<Position>.ComponentType,
        Component<Floor>.ComponentType,
        Component<Direction>.ComponentType,
        Component<Velocity>.ComponentType,
        Component<Movement>.ComponentType,
        Component<Walkable>.ComponentType,
        
        // Combat
        Component<CombatStats>.ComponentType,
        Component<CombatState>.ComponentType,
        
        // Vitals
        Component<Health>.ComponentType,
        Component<Mana>.ComponentType,
        
        // AI
        Component<NpcBrain>.ComponentType,
        Component<NpcPatrol>.ComponentType,
        Component<NpcBehavior>.ComponentType,
        Component<NavigationAgent>.ComponentType,
        Component<NpcPath>.ComponentType,
        
        // Sync
        Component<DirtyFlags>.ComponentType,
    ];
    
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
        Component<Velocity>.ComponentType,
        
        // Projectile
        Component<Components.Projectile>.ComponentType,
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
        Component<Velocity>.ComponentType,
        Component<Health>.ComponentType,
        Component<Mana>.ComponentType,
    ];
}
