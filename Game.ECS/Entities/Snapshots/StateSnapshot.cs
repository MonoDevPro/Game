namespace Game.ECS.Schema.Snapshots;

public readonly record struct StateSnapshot(
    int NetworkId,
    int PosX,
    int PosY,
    sbyte Floor,
    float Speed,
    sbyte DirX,
    sbyte DirY
);