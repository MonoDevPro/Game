using MemoryPack;

namespace Game.DTOs.Game.Player;

[MemoryPackable]
public readonly partial record struct StateData(
    int NetworkId,
    int X, int Y, int Z,
    int DirX, int DirY,
    float Speed
);