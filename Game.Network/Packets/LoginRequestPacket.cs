using Game.Abstractions.Network;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Client -> Server login request with credentials and desired character selection.
/// </summary>
[MemoryPackable]
public partial struct LoginRequestPacket(string username, string password, string? characterName) : IPacket
{
    public string Username { get; set; } = username;
    public string Password { get; set; } = password;
    public string? CharacterName { get; set; } = characterName;
}
