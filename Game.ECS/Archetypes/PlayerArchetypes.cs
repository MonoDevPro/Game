using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Archetypes;

/// <summary>
/// Um arqutipo  um blueprint que define quais componentes uma entidade deve ter.
/// </summary>
public static class PlayerArchetypes
{
    // ============================================
    // Players - Personagens jogáveis (locais e remotos)
    // ============================================
    
    /// <summary>
    /// Arquétipo de jogador completo com todos os componentes necessários.
    /// Inclui componentes para movimento, combate, vitals, input e sincronização.
    /// </summary>
    public static readonly ComponentType[] PlayerArchetype =
    [
        // Set from systems
        Component<NetworkId>.ComponentType, // Assigned by the networking system
        Component<MapId>.ComponentType,     // Assigned by the map management system
        Component<Position>.ComponentType,  // Assigned by the spawn system
        
        // Identity
        Component<PlayerControlled>.ComponentType,
        Component<UniqueID>.ComponentType,
        Component<GenderId>.ComponentType,
        Component<VocationId>.ComponentType,
        
        // Input
        Component<Input>.ComponentType,
        
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