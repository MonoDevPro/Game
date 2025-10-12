using Game.Network.Abstractions;
using Game.Network.Packets.DTOs;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client response to login attempts.
/// </summary>
[MemoryPackable]
public readonly partial struct UnconnectedLoginResponsePacket : IPacket
{
    public bool Success { get; init; }
    public string Message { get; init; }
    public string? SessionToken { get; init; } // ✅ Token de sessão
    public PlayerCharData[] CurrentCharacters { get; init; }

    private UnconnectedLoginResponsePacket(bool success, string message, string? sessionToken, PlayerCharData[] currentCharacters)
    {
        Success = success;
        Message = message;
        SessionToken = sessionToken;
        CurrentCharacters = currentCharacters;
    }

    public static UnconnectedLoginResponsePacket Failure(string message)
        => new(false, message, null, Array.Empty<PlayerCharData>());

    public static UnconnectedLoginResponsePacket SuccessResponse(string sessionToken, PlayerCharData[] characters)
        => new(true, "Login successful", sessionToken, characters);
}
