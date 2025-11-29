using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Entities.Player;

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
        Component<PlayerId>.ComponentType,
        Component<NetworkId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<GenderId>.ComponentType,
        Component<VocationId>.ComponentType,
        Component<Position>.ComponentType,
        Component<Floor>.ComponentType,
        Component<Direction>.ComponentType,
        Component<Speed>.ComponentType,
        Component<Movement>.ComponentType,
        Component<Health>.ComponentType,
        Component<Mana>.ComponentType,
        Component<Walkable>.ComponentType,
        Component<CombatStats>.ComponentType,
        Component<CombatState>.ComponentType,
        Component<Input>.ComponentType,
        Component<DirtyFlags>.ComponentType,
    ];
}