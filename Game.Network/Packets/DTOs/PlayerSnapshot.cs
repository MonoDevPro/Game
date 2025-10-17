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
    public byte Gender { get; set; }
    public byte Vocation { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int PositionZ { get; set; }
    public int FacingX { get; set; }
    public int FacingY { get; set; }
    public float Speed { get; set; }

    public PlayerSnapshot(int networkId, int playerId, int characterId, string name, byte gender, 
        byte vocation, int positionX, int positionY, int positionZ, int facingX, int facingY, float speed)
    {
        NetworkId = networkId;
        PlayerId = playerId;
        CharacterId = characterId;
        Name = name;
        Gender = gender;
        Vocation = vocation;
        PositionX = positionX;
        PositionY = positionY;
        PositionZ = positionZ;
        FacingX = facingX;
        FacingY = facingY;
        Speed = speed;
    }
}