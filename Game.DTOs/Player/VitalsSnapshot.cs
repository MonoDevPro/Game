using MemoryPack;

namespace Game.DTOs.Player;

[MemoryPackable]
public readonly partial record struct VitalsSnapshot(
    int NetworkId,
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp
);