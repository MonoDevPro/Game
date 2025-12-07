using MemoryPack;

namespace Game.DTOs.Game.Player;

[MemoryPackable]
public readonly partial record struct StateData(
    int NetworkId,
    int X, int Y, sbyte Floor,
    float Speed,
    sbyte DirX, sbyte DirY
);