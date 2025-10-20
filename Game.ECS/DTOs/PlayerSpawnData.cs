namespace Game.ECS.DTOs;

public readonly record struct PlayerSpawnData(
    int PlayerId, int NetworkId, 
    int SpawnX, int SpawnY, int SpawnZ, 
    int FacingX, int FacingY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen, 
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense);