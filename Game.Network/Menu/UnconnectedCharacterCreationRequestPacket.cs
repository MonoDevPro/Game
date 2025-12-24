using System.Runtime.InteropServices;
using Game.Domain.Player;
using MemoryPack;

namespace Game.Network.Packets.Menu;

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial record struct UnconnectedCharacterCreationRequestPacket(
    string SessionToken,
    string Name,
    GenderType Gender,
    VocationType Vocation);
