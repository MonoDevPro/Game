using Game.Domain.AI.Enums;

namespace Game.Domain.AI.Data;

/// <summary>
/// Template para criação de Pets.
/// </summary>
public sealed class PetTemplate
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int BaseHealth { get; init; } = 100;
    public int BaseDamage { get; init; } = 10;
    public int AttackRange { get; init; } = 1;
    public float AttackSpeed { get; init; } = 1.0f;
    public float MovementSpeed { get; init; } = 1.2f;
    public PetMode DefaultMode { get; init; } = PetMode.Follow;
    public int FollowDistance { get; init; } = 3;
}