namespace Game.ECS.Entities.Player;

public record PlayerTemplate(
    int NetworkId, int PlayerId, string Name, byte GenderId, byte VocationId,
    sbyte DirX, sbyte DirY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense);