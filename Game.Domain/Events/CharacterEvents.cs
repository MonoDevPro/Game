using Game.Domain.Commons;
using Game.Domain.Enums;

namespace Game.Domain.Events;

/// <summary>
/// Evento disparado quando um personagem sobe de nível.
/// </summary>
public sealed record CharacterLeveledUpEvent : BaseDomainEvent
{
    public int CharacterId { get; init; }
    public int OldLevel { get; init; }
    public int NewLevel { get; init; }
    public long ExperienceGained { get; init; }
}

/// <summary>
/// Evento disparado quando um personagem ganha experiência.
/// </summary>
public sealed record ExperienceGainedEvent : BaseDomainEvent
{
    public int CharacterId { get; init; }
    public long Amount { get; init; }
    public string Source { get; init; } = string.Empty;
}

/// <summary>
/// Evento disparado quando um personagem promove de vocação.
/// </summary>
public sealed record CharacterPromotedEvent : BaseDomainEvent
{
    public int CharacterId { get; init; }
    public VocationType OldVocation { get; init; }
    public VocationType NewVocation { get; init; }
}
