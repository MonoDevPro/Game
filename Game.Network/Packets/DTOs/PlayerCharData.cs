using Game.Domain.Enums;
using MemoryPack;

namespace Game.Network.Packets.DTOs;

[MemoryPackable]
public readonly partial struct PlayerCharData(int id, string name, int level, VocationType vocation, Gender gender)
{
    public int Id { get; init; } = id;
    public string Name { get; init; } = name;
    public int Level { get; init; } = level;
    public VocationType Vocation { get; init; } = vocation;
    public Gender Gender { get; init; } = gender;
}