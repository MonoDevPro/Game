namespace Game.ECS.Components.Primitive;

public readonly record struct GameStats(
    int Hp,
    int Mp,
    int MaxHp,
    int MaxMp,
    float HpRegen,
    float MpRegen,
    int PhysicalAttack,
    int MagicAttack,
    int PhysicalDefense,
    int MagicDefense,
    double AttackSpeed,
    double MovementSpeed
);