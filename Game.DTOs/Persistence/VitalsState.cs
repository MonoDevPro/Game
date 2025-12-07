namespace Game.DTOs.Persistence;

/// <summary>
/// DTO para persistir vitals (HP/MP) do personagem.
/// </summary>
public sealed record VitalsPersistenceDto
{
    public required int CharacterId { get; init; }
    public required int CurrentHp { get; init; }
    public required int CurrentMp { get; init; }
}