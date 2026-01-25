using Game.Domain;

namespace Game.Infrastructure.EfCore;

public sealed class AccountRow
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

public sealed class CharacterRow
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte Gender { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Floor { get; set; }
    public int DirX { get; set; }
    public int DirY { get; set; }
}

public sealed class CharacterVocationRow
{
    public int Id { get; set; }
    public int CharacterId { get; set; }
    public byte Vocation { get; set; }
    public int Level { get; set; }
    public long Experience { get; set; }
    public int Strength { get; set; }
    public int Endurance { get; set; }
    public int Agility { get; set; }
    public int Intelligence { get; set; }
    public int Willpower { get; set; }
    public int HealthPoints { get; set; }
    public int ManaPoints { get; set; }
}

public sealed class EnterTicketRow
{
    public string Ticket { get; set; } = string.Empty;
    public int CharacterId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}