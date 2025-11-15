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
    /// Suporta movimento, combate, mas não tem input de jogador.
    /// </summary>
    public static readonly ComponentType[] NPCCharacter =
    [
        Component<NetworkId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<Position>.ComponentType,
        Component<Facing>.ComponentType,
        Component<Velocity>.ComponentType,
        Component<Movement>.ComponentType,
        Component<Health>.ComponentType,
        Component<Walkable>.ComponentType,
        Component<Attackable>.ComponentType,
        Component<AttackPower>.ComponentType,
        Component<Defense>.ComponentType,
        Component<CombatState>.ComponentType,
        Component<AIControlled>.ComponentType,
        Component<DirtyFlags>.ComponentType
    ];
}