using Game.Domain.AI.Enums;

namespace Game.Domain.AI.ValueObjects;

/// <summary>
/// Comportamento de Pet.
/// </summary>
public struct PetBehavior
{
    public PetMode Mode;
    public int FollowDistance;
    public int AttackRange;
}