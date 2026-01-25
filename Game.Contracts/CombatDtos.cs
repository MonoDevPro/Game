using MemoryPack;

namespace Game.Contracts;

public enum CombatEventType : byte
{
    AttackStarted = 1,
    Hit = 2,
    ProjectileSpawn = 3
}

/// <summary>
/// Evento simples de combate para sincronização.
/// </summary>
[MemoryPackable]
public readonly partial record struct CombatEvent(
    CombatEventType Type,
    int AttackerId,
    int TargetId,
    int DirX,
    int DirY,
    int Damage,
    int X,
    int Y,
    int Floor,
    float Speed,
    int Range);

/// <summary>
/// Lote de eventos de combate do servidor.
/// </summary>
[MemoryPackable]
public readonly partial record struct CombatEventBatch(
    long ServerTick,
    List<CombatEvent> Events) : IEnvelopePayload;
