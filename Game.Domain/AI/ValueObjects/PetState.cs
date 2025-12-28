namespace Game.Domain.AI.ValueObjects;

/// <summary>
/// Estado do Pet.
/// </summary>
public struct PetState
{
    public bool IsChasing;
    public bool IsAttacking;
    public int TargetEntityId;
}
