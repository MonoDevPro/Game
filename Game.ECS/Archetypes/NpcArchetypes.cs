using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Archetypes;

public static class NpcArchetypes
{
    /// <summary>
    /// Full archetype for NPC entities with AI components.
    /// Includes identity, transform, combat, vitals, AI, and synchronization components.
    /// </summary>
    public static readonly ComponentType[] NPC =
    [
        // Set from systems
        Component<NetworkId>.ComponentType, // Assigned by the networking system
        Component<MapId>.ComponentType,     // Assigned by the map management system
        Component<Position>.ComponentType,  // Assigned by the spawn system
        
        // Identity
        Component<AIControlled>.ComponentType,
        Component<UniqueID>.ComponentType,
        Component<GenderId>.ComponentType,
        Component<VocationId>.ComponentType,
        
        // AI
        Component<Brain>.ComponentType,
        Component<AIBehaviour>.ComponentType,
        Component<NavigationAgent>.ComponentType,
        
        // Transform
        Component<Direction>.ComponentType,
        Component<Speed>.ComponentType,
        
        // Movement
        Component<Walkable>.ComponentType,
        Component<SpatialAnchor>.ComponentType,
        
        // Combat
        Component<CombatStats>.ComponentType,
        Component<CombatState>.ComponentType,
        
        // Vitals
        Component<Health>.ComponentType,
        Component<Mana>.ComponentType,
        
        // Lifecycle
        Component<SpawnPoint>.ComponentType,
    ];
}