using MemoryPack;

namespace Game.DTOs.Player;

[MemoryPackable]
public readonly partial record struct StateSnapshot(
    int NetworkId,
    int X, int Y, int Z,
    int DirX, int DirY
);