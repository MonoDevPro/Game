using Game.Domain.Enums;
using Game.Domain.VOs;
using MemoryPack;

namespace Game.Network.Packets.DTOs;

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
    public Gender Gender { get; set; }
    public VocationType Vocation { get; set; }
    public Coordinate Position { get; set; }
    public Coordinate Facing { get; set; }
    public float Speed { get; set; }

    public PlayerSnapshot(int networkId, int playerId, int characterId, string name, Gender gender, 
        VocationType vocation, Coordinate position, Coordinate facing, float speed)
    {
        NetworkId = networkId;
        PlayerId = playerId;
        CharacterId = characterId;
        Name = name;
        Gender = gender;
        Vocation = vocation;
        Position = position;
        Facing = facing;
        Speed = speed;
    }
}