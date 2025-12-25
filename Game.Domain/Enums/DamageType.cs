namespace Game.Domain.Enums;

/// <summary>
/// Tipo de dano primário.
/// </summary>
[System.Flags]
public enum DamageType : byte
{
    None = 0,
    Physical = 1 << 0,
    Magical = 1 << 1,
    True = 1 << 2, // Dano que ignora defesas
}