using Game.Domain.Enums;
using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets;

[MemoryPackable]
public readonly partial record struct UnconnectedCharacterDeleteRequestPacket(
    string SessionToken,
    int CharacterId) : IPacket;
