using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Entities.Archetypes;

public static partial class GameArchetypes
{
    // ============================================
    // NPCs - Personagens controlados por IA
    // ============================================
    
    /// <summary>
    /// Arquétipo de NPC com IA.
    /// Suporta movimento, combate, pathfinding A*, mas não tem input de jogador.
    /// </summary>
    public static readonly ComponentType[] NPCCharacter =
    [
        Component<NetworkId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<Position>.ComponentType,
        Component<Floor>.ComponentType,
        Component<Facing>.ComponentType,
        Component<Velocity>.ComponentType,
        Component<Movement>.ComponentType,
        Component<Health>.ComponentType,
        Component<Mana>.ComponentType,
        Component<Walkable>.ComponentType,
        Component<CombatStats>.ComponentType,
        Component<CombatState>.ComponentType,
        Component<Input>.ComponentType,
        Component<DirtyFlags>.ComponentType,
        Component<AIControlled>.ComponentType,
        Component<NpcInfo>.ComponentType,
        Component<NpcPatrol>.ComponentType,
        Component<NpcPath>.ComponentType,
        Component<NpcType>.ComponentType,
        Component<NpcBrain>.ComponentType,
        Component<NavigationAgent>.ComponentType,
    ];
}