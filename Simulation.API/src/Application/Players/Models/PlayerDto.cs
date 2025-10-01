namespace GameWeb.Application.Players.Models;

public enum Gender : byte
{
    None, 
    Male, 
    Female
}

public enum Vocation : byte
{
    None,
    /// <summary>
    /// Warrior class - melee combat specialist
    /// </summary>
    Warrior,
    /// <summary>
    /// Mage class - magic damage dealer
    /// </summary>
    Mage,
    /// <summary>
    /// Archer class - ranged physical damage dealer
    /// </summary>
    Archer
}

public record PlayerDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public Gender Gender { get; init; }
    public Vocation Vocation { get; init; }

    public int HealthMax { get; init; }
    public int HealthCurrent { get; init; }
    public int AttackDamage { get; init; }
    public int AttackRange { get; init; }
    public float AttackCastTime { get; init; }
    public float AttackCooldown { get; init; }
    public float MoveSpeed { get; init; }

    // World state (discrete grid)
    public int PosX { get; init; }
    public int PosY { get; init; }

    // Direction vector as integers (e.g. -1/0/1). Keep small for network efficiency.
    public int DirX { get; init; }
    public int DirY { get; init; }
}
