namespace Game.ECS.Schema.Snapshots;

public readonly record struct VitalsSnapshot(
    int NetworkId, 
    int Hp, 
    int MaxHp, 
    int Mp, 
    int MaxMp);