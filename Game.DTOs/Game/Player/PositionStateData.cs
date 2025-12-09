using MemoryPack;

namespace Game.DTOs.Game.Player;

[MemoryPackable]
public readonly partial record struct PositionStateData(
    int NetworkId,
    int X, int Y, sbyte Floor,
    sbyte DirX, sbyte DirY
);