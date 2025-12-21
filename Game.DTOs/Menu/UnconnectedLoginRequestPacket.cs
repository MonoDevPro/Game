using MemoryPack;

namespace Game.Network.Packets.Menu;

/// <summary>
/// Client -> Server login request with credentials and desired character selection.
/// </summary>
[MemoryPackable]
public readonly partial record struct UnconnectedLoginRequestPacket(
    string Username,
    string Password);
