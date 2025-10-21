using Game.ECS.Entities;
using MemoryPack;

namespace Game.ECS.Components;

// ============================================
// Player Snapshots
// ============================================

[MemoryPackable]
public readonly partial record struct PlayerInputSnapshot(
    int NetworkId,
    sbyte InputX, 
    sbyte InputY, 
    InputFlags Flags);

[MemoryPackable]
public readonly partial record struct PlayerStateSnapshot(
    int NetworkId,
    int PositionX, 
    int PositionY, 
    int PositionZ, 
    int FacingX, 
    int FacingY, 
    float Speed);

[MemoryPackable]
public readonly partial record struct PlayerVitalsSnapshot(
    int NetworkId, 
    int CurrentHp, 
    int MaxHp, 
    int CurrentMp, 
    int MaxMp);

[MemoryPackable]
public readonly partial record struct PlayerDespawn(int NetworkId);

// ============================================
// NPC Snapshots
// ============================================

[MemoryPackable]
public readonly partial record struct NPCStateSnapshot(
    int NetworkId,
    int PositionX,
    int PositionY,
    int PositionZ,
    int FacingX,
    int FacingY,
    float Speed,
    int CurrentHp,
    int MaxHp);

[MemoryPackable]
public readonly partial record struct NPCSpawn(
    int NetworkId,
    string Name,
    int PositionX,
    int PositionY,
    int PositionZ,
    int CurrentHp,
    int MaxHp);

[MemoryPackable]
public readonly partial record struct NPCDespawn(int NetworkId);

// ============================================
// Projectile Snapshots
// ============================================

[MemoryPackable]
public readonly partial record struct ProjectileStateSnapshot(
    int NetworkId,
    int ShooterId,
    int PositionX,
    int PositionY,
    int PositionZ,
    int DirectionX,
    int DirectionY,
    float Speed,
    int Damage);

[MemoryPackable]
public readonly partial record struct ProjectileHit(
    int ProjectileNetworkId,
    int TargetNetworkId,
    int Damage);

// ============================================
// Combat Event Snapshots
// ============================================

[MemoryPackable]
public readonly partial record struct AttackEventSnapshot(
    int AttackerId,
    int TargetId,
    int Damage,
    bool IsMagical);

[MemoryPackable]
public readonly partial record struct HealEventSnapshot(
    int HealerId,
    int TargetId,
    int Amount);

[MemoryPackable]
public readonly partial record struct DeathEventSnapshot(
    int DeadNetworkId,
    int? KillerNetworkId);
