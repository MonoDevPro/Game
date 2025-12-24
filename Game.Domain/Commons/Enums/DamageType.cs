namespace Game.Domain.Commons.Enums;

/// <summary>
/// Tipo de dano primário.
/// </summary>
public enum DamageType : byte
{
    Physical,
    Magical,
    True,  // Ignora defesas
}