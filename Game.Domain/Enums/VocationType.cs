namespace Game.Domain.Enums;

/// <summary>
/// Vocações base do jogo.
/// </summary>
public enum VocationType : byte
{
    None = 0,
    Warrior = 1,
    Archer = 2,
    Mage = 3,
    Cleric = 4,
}

/// <summary>
/// Arquétipo de combate da vocação.
/// </summary>
public enum VocationArchetype : byte
{
    None = 0,
    Melee = 1,
    Ranged = 2,
    Magic = 3,
    Hybrid = 4,
}