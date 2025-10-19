using MemoryPack;

namespace Game.Network.Packets.Menu;

/// <summary>
/// Client -> Server request to create a new account and starter character.
/// </summary>
[MemoryPackable]
public partial struct UnconnectedRegistrationRequestPacket(
    string username,
    string email,
    string password)
{
    public string Username { get; set; } = username;
    public string Email { get; set; } = email;
    public string Password { get; set; } = password;
}
