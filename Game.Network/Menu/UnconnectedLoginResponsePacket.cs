using MemoryPack;

namespace Game.Network.Packets.Menu;

/// <summary>
/// Server -> Client response to login attempts.
/// </summary>
[MemoryPackable]
public readonly partial record struct UnconnectedLoginResponsePacket(
    bool Success, string Message, string? SessionToken, DTOs.Menu.CharMenuData[] CurrentCharacters)
{
    public static UnconnectedLoginResponsePacket Failure(string message)
        => new(false, message, null, Array.Empty<DTOs.Menu.CharMenuData>());

    public static UnconnectedLoginResponsePacket SuccessResponse(string sessionToken, DTOs.Menu.CharMenuData[] characters)
        => new(true, "Login successful", sessionToken, characters);
}
