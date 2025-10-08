using System;
using Game.Abstractions.Network;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client response to login attempts.
/// </summary>
[MemoryPackable]
public partial struct LoginResponsePacket : IPacket
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PlayerSnapshot LocalPlayer { get; set; }
    public PlayerSnapshot[] OnlinePlayers { get; set; } = [];

    private LoginResponsePacket(bool success, string message, PlayerSnapshot localPlayer, PlayerSnapshot[] onlinePlayers)
    {
        Success = success;
        Message = message;
        LocalPlayer = localPlayer;
        OnlinePlayers = onlinePlayers;
    }

    public static LoginResponsePacket Failure(string message)
        => new(false, message, default, []);

    public static LoginResponsePacket SuccessResponse(PlayerSnapshot localPlayer, PlayerSnapshot[] onlinePlayers)
        => new(true, string.Empty, localPlayer, onlinePlayers);
}
