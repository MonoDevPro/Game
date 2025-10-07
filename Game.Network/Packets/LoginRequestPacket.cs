using Game.Abstractions.Network;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Client -> Server login request with credentials and desired character selection.
/// </summary>
[MemoryPackable]
public partial struct LoginRequestPacket : IPacket
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string? CharacterName { get; set; }

    public LoginRequestPacket(string username, string password, string? characterName)
    {
        Username = username;
        Password = password;
        CharacterName = characterName;
    }
}
