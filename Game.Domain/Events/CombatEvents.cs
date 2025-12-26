using Game.Domain.Commons;
using Game.Domain.Enums;

namespace Game.Domain.Events;

/// <summary>
/// Evento disparado quando uma entidade recebe dano.
/// </summary>
public sealed record DamageTakenEvent : BaseDomainEvent
{
    public int VictimId { get; init; }
    public int AttackerId { get; init; }
    public int Damage { get; init; }
    public DamageType DamageType { get; init; }
    public bool IsCritical { get; init; }
    public bool IsLethal { get; init; }
}
