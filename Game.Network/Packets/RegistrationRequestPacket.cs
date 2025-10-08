using Game.Abstractions.Network;
using Game.Domain.Enums;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Client -> Server request to create a new account and starter character.
/// </summary>
[MemoryPackable]
public partial struct RegistrationRequestPacket(
    string username,
    string email,
    string password,
    string characterName,
    Gender gender,
    VocationType vocation)
    : IPacket
{
    public string Username { get; set; } = username;
    public string Email { get; set; } = email;
    public string Password { get; set; } = password;
    public string CharacterName { get; set; } = characterName;
    public Gender Gender { get; set; } = gender;
    public VocationType Vocation { get; set; } = vocation;
}
