using MemoryPack;

namespace Game.Network.Packets.Menu;

/// <summary>
/// Client -> Server request to create a new account and starter character.
/// </summary>
[MemoryPackable]
public readonly partial record struct UnconnectedRegistrationRequestPacket(
    string Username,
    string Email,
    string Password);
