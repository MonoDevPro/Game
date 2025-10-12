using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client acknowledgement for registration attempts.
/// </summary>
[MemoryPackable]
public partial struct UnconnectedRegistrationResponsePacket(bool success, string message) : IPacket
{
    public bool Success { get; set; } = success;
    public string Message { get; set; } = message;

    public static UnconnectedRegistrationResponsePacket Failure(string message) => new(false, message);
    public static UnconnectedRegistrationResponsePacket Ok() => new(true, string.Empty);
}
