using Game.Domain.Enums;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Flat representation of a player's visible state for sync packets.
/// </summary>
[MemoryPackable]
public partial struct PlayerSnapshot
{
    public int NetworkId { get; set; }
    public int PlayerId { get; set; }
    public int CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public GridPosition Position { get; set; }
    public DirectionEnum Facing { get; set; }

    public PlayerSnapshot(int networkId, int playerId, int characterId, string name, GridPosition position, DirectionEnum facing)
    {
        NetworkId = networkId;
        PlayerId = playerId;
        CharacterId = characterId;
        Name = name;
        Position = position;
        Facing = facing;
    }
}
