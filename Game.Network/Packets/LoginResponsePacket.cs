using Game.Network.Abstractions;
using Game.Network.Packets.DTOs;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client response to login attempts.
/// </summary>
[MemoryPackable]
public readonly partial struct LoginResponsePacket : IPacket
{
    public bool Success { get; init; }
    public string Message { get; init; }
    public PlayerCharData[] CurrentCharacters { get; init; }

    private LoginResponsePacket(bool success, string message, PlayerCharData[] currentCharacters)
    {
        Success = success;
        Message = message;
        CurrentCharacters = currentCharacters;
    }

    public static LoginResponsePacket Failure(string message)
        => new(false, message, []);

    public static LoginResponsePacket SuccessResponse(DTOs.PlayerCharData[] currentCharacters)
        => new(true, string.Empty, currentCharacters);
}
