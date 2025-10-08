using Game.Abstractions.Network;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client acknowledgement for registration attempts.
/// </summary>
[MemoryPackable]
public partial struct RegistrationResponsePacket(bool success, string message) : IPacket
{
    public bool Success { get; set; } = success;
    public string Message { get; set; } = message;

    public static RegistrationResponsePacket Failure(string message) => new(false, message);
    public static RegistrationResponsePacket Ok() => new(true, string.Empty);
}
