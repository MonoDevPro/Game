namespace Game.Domain.Data;

/// <summary>
/// DTO para persistir stats completos do personagem.
/// </summary>
public sealed record StatsState
{
    public required int CharacterId { get; init; }
    public required int Level { get; init; }
    public required long Experience { get; init; }
    public required int BaseStrength { get; init; }
    public required int BaseDexterity { get; init; }
    public required int BaseIntelligence { get; init; }
    public required int BaseConstitution { get; init; }
    public required int BaseSpirit { get; init; }
}