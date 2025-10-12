using Game.Domain.Enums;
using MemoryPack;

namespace Game.Network.Packets.DTOs;

[MemoryPackable]
public readonly partial record struct PlayerCharData(
    int Id,
    string Name,
    int Level,
    VocationType Vocation,
    Gender Gender);
