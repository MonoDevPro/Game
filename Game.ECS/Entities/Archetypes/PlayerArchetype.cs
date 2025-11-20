using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Entities.Archetypes;

/// <summary>
/// Um arqutipo  um blueprint que define quais componentes uma entidade deve ter.
/// </summary>
public static partial class GameArchetypes
{
    // ============================================
    // Players - Personagens jogáveis (locais e remotos)
    // ============================================
    
    /// <summary>
    /// Arquétipo de jogador completo com todos os componentes necessários.
    /// Inclui componentes para movimento, combate, vitals, input e sincronização.
    /// </summary>
    public static readonly ComponentType[] PlayerCharacter =
    [
        Component<NetworkId>.ComponentType,
        Component<PlayerId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<PlayerInfo>.ComponentType,
        Component<Position>.ComponentType,
        Component<Facing>.ComponentType,
        Component<Velocity>.ComponentType,
        Component<Movement>.ComponentType,
        Component<Health>.ComponentType,
        Component<Mana>.ComponentType,
        Component<Walkable>.ComponentType,
        Component<Attackable>.ComponentType,
        Component<AttackPower>.ComponentType,
        Component<Defense>.ComponentType,
        Component<CombatState>.ComponentType,
        Component<Input>.ComponentType,
        Component<PlayerControlled>.ComponentType,
        Component<DirtyFlags>.ComponentType
    ];
}