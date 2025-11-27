using MemoryPack;

namespace Game.Network.Packets.Game;

// Usado apenas para ID interno
[MemoryPackable]
public readonly partial record struct NpcIdentity(int NetworkId, ushort TemplateId);

// O pacote "Gordo" de Spawn (Atomicidade)
[MemoryPackable]
public readonly partial record struct NpcSpawnRequest(
    int NetworkId,
    ushort TemplateId,
    int X,
    int Y,
    sbyte Floor,
    sbyte DirectionX,
    sbyte DirectionY,
    int CurrentHp,
    int MaxHp
);

// Usado para mover durante o jogo (Frequente)
[MemoryPackable]
public readonly partial record struct NpcStateUpdate(
    int NetworkId,
    int X,
    int Y,
    float Speed,
    sbyte DirectionX,
    sbyte DirectionY
);

// Usado quando leva dano/cura (Pouco Frequente)
[MemoryPackable]
public readonly partial record struct NpcVitalsUpdate(
    int NetworkId,
    int CurrentHp,
    int CurrentMp
);