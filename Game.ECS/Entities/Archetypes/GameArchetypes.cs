using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Entities.Archetypes;

/// <summary>
/// Define os arcétipos (composições de componentes) para diferentes entidades no jogo.
/// Um arquétipo é um blueprint que define quais componentes uma entidade deve ter.
/// </summary>
public static class GameArchetypes
{
    // ============================================
    // Players - Personagens jogáveis (locais e remotos)
    // ============================================
    
    /// <summary>
    /// Arquétipo de jogador completo com todos os componentes necessários.
    /// Inclui componentes para movimento, combate, vitals, input e sincronização.
    /// </summary>
    public static readonly ComponentType[] PlayerCharacter = new[]
    {
        Component<NetworkId>.ComponentType,
        Component<PlayerId>.ComponentType,
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
        Component<NetworkDirty>.ComponentType,
        Component<PlayerInput>.ComponentType,
        Component<PlayerControlled>.ComponentType,
    };

    // ============================================
    // NPCs - Personagens controlados por IA
    // ============================================
    
    /// <summary>
    /// Arquétipo de NPC com IA.
    /// Suporta movimento, combate, mas não tem input de jogador.
    /// </summary>
    public static readonly ComponentType[] NPCCharacter = new[]
    {
        Component<NetworkId>.ComponentType,
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
        Component<NetworkDirty>.ComponentType,
        Component<AIControlled>.ComponentType,
    };

    // ============================================
    // Projectiles - Projéteis
    // ============================================
    
    /// <summary>
    /// Arquétipo de projétil (bala, flecha, magia).
    /// Mais simples que NPCs: apenas movimento e dano.
    /// </summary>
    public static readonly ComponentType[] Projectile = new[]
    {
        Component<NetworkId>.ComponentType,
        Component<Position>.ComponentType,
        Component<Facing>.ComponentType,
        Component<Velocity>.ComponentType,
        Component<AttackPower>.ComponentType,
        Component<NetworkDirty>.ComponentType,
    };

    // ============================================
    // Items - Itens soltos no mapa
    // ============================================
    
    /// <summary>
    /// Arquétipo de item solto no chão.
    /// Apenas posição e sincronização de rede.
    /// </summary>
    public static readonly ComponentType[] DroppedItem = new[]
    {
        Component<NetworkId>.ComponentType,
        Component<Position>.ComponentType,
        Component<NetworkDirty>.ComponentType,
    };

    // ============================================
    // Objects - Objetos estáticos/interativos
    // ============================================
    
    /// <summary>
    /// Arquétipo de objeto interativo (porta, baú, etc).
    /// Apenas posição, sem sincronização ativa.
    /// </summary>
    public static readonly ComponentType[] InteractiveObject = new[]
    {
        Component<NetworkId>.ComponentType,
        Component<Position>.ComponentType,
    };
}