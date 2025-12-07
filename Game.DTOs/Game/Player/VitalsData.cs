using MemoryPack;

namespace Game.DTOs.Game.Player;

[MemoryPackable]
public readonly partial record struct VitalsData(
    int NetworkId,
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp
);