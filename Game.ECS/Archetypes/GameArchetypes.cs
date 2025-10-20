using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Archetypes;

public static class GameArchetypes
{
    // Jogador completo
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
        Component<AttackPower>.ComponentType,
        Component<Defense>.ComponentType,
        Component<CombatState>.ComponentType,
        Component<NetworkDirty>.ComponentType,
        Component<PlayerInput>.ComponentType,
        Component<PlayerControlled>.ComponentType,
    };

    // NPC com IA
    public static readonly ComponentType[] NPCCharacter = new[]
    {
        Component<NetworkId>.ComponentType,
        Component<Position>.ComponentType,
        Component<Facing>.ComponentType,
        Component<Velocity>.ComponentType,
        Component<Health>.ComponentType,
        Component<Walkable>.ComponentType,
        Component<AttackPower>.ComponentType,
        Component<Defense>.ComponentType,
        Component<CombatState>.ComponentType,
        Component<NetworkDirty>.ComponentType,
        Component<AIControlled>.ComponentType,
    };

    // Projétil (bala, flecha, magia)
    public static readonly ComponentType[] Projectile = new[]
    {
        Component<NetworkId>.ComponentType,
        Component<Position>.ComponentType,
        Component<Facing>.ComponentType,
        Component<Velocity>.ComponentType,
        Component<AttackPower>.ComponentType,
        Component<NetworkDirty>.ComponentType,
    };

    // Item no chão
    public static readonly ComponentType[] DroppedItem = new[]
    {
        Component<NetworkId>.ComponentType,
        Component<NetworkDirty>.ComponentType,
    };
}