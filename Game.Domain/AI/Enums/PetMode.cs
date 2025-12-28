namespace Game.Domain.AI.Enums;

/// <summary>
/// Modo do pet.
/// </summary>
public enum PetMode : byte
{
    Follow = 0,
    Stay = 1,
    Aggressive = 2,
    Defensive = 3,
    Passive = 4,
    Attack = 5,
    Defend = 6
}