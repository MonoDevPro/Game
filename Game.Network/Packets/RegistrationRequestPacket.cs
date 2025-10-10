using Game.Domain.Enums;
using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Client -> Server request to create a new account and starter character.
/// </summary>
[MemoryPackable]
public partial struct RegistrationRequestPacket(
    string username,
    string email,
    string password)
    : IPacket
{
    public string Username { get; set; } = username;
    public string Email { get; set; } = email;
    public string Password { get; set; } = password;
}
