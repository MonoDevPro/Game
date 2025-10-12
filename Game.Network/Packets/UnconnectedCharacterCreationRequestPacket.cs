using Game.Domain.Enums;
using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets;

[MemoryPackable]
public readonly partial struct UnconnectedCharacterCreationRequestPacket(string sessionToken, string name, Gender gender, VocationType vocation) : IPacket
{
    public string SessionToken { get; init; } = sessionToken;
    public string Name { get; init; } = name;
    public Gender Gender { get; init; } = gender;
    public VocationType Vocation { get; init; } = vocation;
}