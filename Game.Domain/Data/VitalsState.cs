namespace Game.Domain.Data;

/// <summary>
/// DTO para persistir vitals (HP/MP) do personagem.
/// </summary>
public sealed record VitalsState
{
    public required int CharacterId { get; init; }
    public required int CurrentHp { get; init; }
    public required int CurrentMp { get; init; }
}