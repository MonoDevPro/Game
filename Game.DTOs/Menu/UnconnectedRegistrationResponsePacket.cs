using MemoryPack;

namespace Game.Network.Packets.Menu;

/// <summary>
/// Server -> Client acknowledgement for registration attempts.
/// </summary>
[MemoryPackable]
public readonly partial record struct UnconnectedRegistrationResponsePacket(
    bool Success, 
    string Message)
{
    public static UnconnectedRegistrationResponsePacket Failure(string message) => new(false, message);
    public static UnconnectedRegistrationResponsePacket Ok() => new(true, string.Empty);
}
