using MemoryPack;

namespace Game.Network.Packets.Menu;

/// <summary>
/// Client -> Server login request with credentials and desired character selection.
/// </summary>
[MemoryPackable]
public partial struct UnconnectedLoginRequestPacket(string username, string password)
{
    public string Username { get; set; } = username;
    public string Password { get; set; } = password;
}
