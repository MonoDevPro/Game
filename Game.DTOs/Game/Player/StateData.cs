using MemoryPack;

namespace Game.DTOs.Game.Player;

[MemoryPackable]
public readonly partial record struct StateData(
    int NetworkId,
    int X, int Y, int Z,
    sbyte DirX, sbyte DirY,
    float Speed
);