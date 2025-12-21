namespace GameECS.Modules.Combat.Shared.Data;

/// <summary>
/// Estatísticas base por vocação.
/// </summary>
public readonly struct VocationStats
{
    public int BaseHealth { get; init; }
    public int BaseMana { get; init; }
    public int BasePhysicalDamage { get; init; }
    public int BaseMagicDamage { get; init; }
    public int BasePhysicalDefense { get; init; }
    public int BaseMagicDefense { get; init; }
    public int BaseAttackRange { get; init; }
    public float BaseAttackSpeed { get; init; }  // Multiplicador (1.0 = normal)
    public float BaseCriticalChance { get; init; }
    public int ManaCostPerAttack { get; init; }

    /// <summary>
    /// Stats do Knight: Alta vida, defesa física, dano melee moderado.
    /// </summary>
    public static VocationStats Knight => new()
    {
        BaseHealth = 150,
        BaseMana = 30,
        BasePhysicalDamage = 25,
        BaseMagicDamage = 5,
        BasePhysicalDefense = 20,
        BaseMagicDefense = 8,
        BaseAttackRange = 1,  // Melee
        BaseAttackSpeed = 1.0f,
        BaseCriticalChance = 5f,
        ManaCostPerAttack = 0
    };

    /// <summary>
    /// Stats do Mage: Baixa vida, alto dano mágico, range médio.
    /// </summary>
    public static VocationStats Mage => new()
    {
        BaseHealth = 80,
        BaseMana = 150,
        BasePhysicalDamage = 5,
        BaseMagicDamage = 35,
        BasePhysicalDefense = 5,
        BaseMagicDefense = 15,
        BaseAttackRange = 6,  // Ranged magic
        BaseAttackSpeed = 0.8f,
        BaseCriticalChance = 8f,
        ManaCostPerAttack = 10
    };

    /// <summary>
    /// Stats do Archer: Vida média, dano físico à distância, alta velocidade.
    /// </summary>
    public static VocationStats Archer => new()
    {
        BaseHealth = 100,
        BaseMana = 50,
        BasePhysicalDamage = 30,
        BaseMagicDamage = 8,
        BasePhysicalDefense = 10,
        BaseMagicDefense = 10,
        BaseAttackRange = 8,  // Long range
        BaseAttackSpeed = 1.3f,
        BaseCriticalChance = 12f,
        ManaCostPerAttack = 0
    };

    public static VocationStats GetForVocation(VocationType vocation) => vocation switch
    {
        VocationType.Knight => Knight,
        VocationType.Mage => Mage,
        VocationType.Archer => Archer,
        _ => Knight
    };
}
