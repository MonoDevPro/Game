using MemoryPack;

namespace Game.Network.Packets.Menu;

/// <summary>
/// Server -> Client response to login attempts.
/// </summary>
[MemoryPackable]
public readonly partial record struct UnconnectedLoginResponsePacket(
    bool Success, string Message, string? SessionToken, CharMenuData[] CurrentCharacters)
{
    public static UnconnectedLoginResponsePacket Failure(string message)
        => new(false, message, null, Array.Empty<CharMenuData>());

    public static UnconnectedLoginResponsePacket SuccessResponse(string sessionToken, CharMenuData[] characters)
        => new(true, "Login successful", sessionToken, characters);
}
