using MemoryPack;

namespace Game.ECS.Services.Snapshot.Data;

[MemoryPackable]
public readonly partial record struct PlayerVitalSnapshot(
    int NetworkId,
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp
);

[MemoryPackable]
public readonly partial record struct PlayerVitalPacket(PlayerVitalSnapshot[] Vitals);