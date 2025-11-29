using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Entities.Npc;

public static class NpcArchetypes
{
    /// <summary>
    /// Full archetype for NPC entities with AI components.
    /// Includes identity, transform, combat, vitals, AI, and synchronization components.
    /// </summary>
    public static readonly ComponentType[] NPC =
    [
        // Identity
        Component<NetworkId>.ComponentType,
        Component<NpcId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<NameHandle>.ComponentType,
        Component<GenderId>.ComponentType,
        Component<VocationId>.ComponentType,
        
        // Transform
        Component<Position>.ComponentType,
        Component<Floor>.ComponentType,
        Component<Direction>.ComponentType,
        Component<Speed>.ComponentType,
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
        Component<NpcBehavior>.ComponentType,
        Component<NavigationAgent>.ComponentType,
        
        // Sync
        Component<DirtyFlags>.ComponentType,
    ];
}