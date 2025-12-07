namespace Game.DTOs.Persistence;

/// <summary>
/// Snapshot reutilizável de posição/direção para evitar duplicação de campos.
/// </summary>
public readonly record struct PositionState(
    int PositionX,
    int PositionY,
    sbyte Floor,
    sbyte FacingX,
    sbyte FacingY);