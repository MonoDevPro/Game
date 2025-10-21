using MemoryPack;

namespace Game.ECS.Entities.Data;

/// <summary>
/// Dados completos de um jogador (usado no spawn).
/// </summary>
[MemoryPackable]
public readonly record struct PlayerCharacter(
    int PlayerId, int NetworkId,
    int SpawnX, int SpawnY, int SpawnZ,
    int FacingX, int FacingY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense);

/// <summary>
/// Dados de um NPC (controlado por IA).
/// </summary>
[MemoryPackable]
public readonly record struct NPCCharacter(
    int NetworkId,
    string Name,
    int PositionX, int PositionY, int PositionZ,
    int Hp, int MaxHp, float HpRegen,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense);

/// <summary>
/// Dados de um projétil.
/// </summary>
[MemoryPackable]
public readonly record struct ProjectileData(
    int NetworkId,
    int ShooterId,
    int StartX, int StartY, int StartZ,
    int DirectionX, int DirectionY,
    float Speed,
    int PhysicalDamage, int MagicalDamage);

/// <summary>
/// Dados de um item solto no mapa.
/// </summary>
[MemoryPackable]
public readonly record struct DroppedItemData(
    int NetworkId,
    int ItemId,
    int PositionX, int PositionY, int PositionZ,
    int Quantity);