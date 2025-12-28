namespace Game.Domain.Player.ValueObjects;

/// <summary>
/// Ownership de player (para persistência).
/// </summary>
public struct PlayerOwnership
{
    public int AccountId;
    public int CharacterId;
}