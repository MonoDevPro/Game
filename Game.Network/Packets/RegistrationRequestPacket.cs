using Game.Abstractions.Network;
using Game.Domain.Enums;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Client -> Server request to create a new account and starter character.
/// </summary>
[MemoryPackable]
public partial struct RegistrationRequestPacket : IPacket
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string CharacterName { get; set; }
    public Gender Gender { get; set; }
    public VocationType Vocation { get; set; }

    public RegistrationRequestPacket(string username, string email, string password, string characterName, Gender gender, VocationType vocation)
    {
        Username = username;
        Email = email;
        Password = password;
        CharacterName = characterName;
        Gender = gender;
        Vocation = vocation;
    }
}
