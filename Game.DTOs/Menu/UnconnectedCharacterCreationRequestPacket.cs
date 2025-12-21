using System.Runtime.InteropServices;
using Game.ECS.Shared.Components.Entities;
using MemoryPack;

namespace Game.Network.Packets.Menu;

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial record struct UnconnectedCharacterCreationRequestPacket(
    string SessionToken,
    string Name,
    Gender Gender,
    VocationType Vocation);
