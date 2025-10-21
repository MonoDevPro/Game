using MemoryPack;

namespace Game.ECS.Entities.Data;

[MemoryPackable]
public readonly record struct PlayerCharacter(
    int PlayerId, int NetworkId,
    string Name, int Level, int ClassId,
    int SpawnX, int SpawnY, int SpawnZ,
    int FacingX, int FacingY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense);