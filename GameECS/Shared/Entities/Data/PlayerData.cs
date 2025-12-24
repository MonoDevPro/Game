namespace GameECS.Shared.Entities.Data;

/// <summary>
/// Dados persistidos do player.
/// </summary>
public sealed class PlayerData
{
    public int AccountId { get; set; }
    public int CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public long Experience { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int MapId { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }
    public DateTime LastLogin { get; set; }
    public DateTime LastSave { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
}