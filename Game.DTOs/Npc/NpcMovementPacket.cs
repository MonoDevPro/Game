using MemoryPack;

namespace Game.DTOs.Npc;

/// <summary>
/// Snapshot de movimento de NPC para sincronização com clientes.
/// Contém posição atual, destino e estado do movimento.
/// </summary>
[MemoryPackable]
public readonly partial record struct NpcMovementSnapshot(
    int NetworkId,
    short CurrentX,
    short CurrentY,
    short CurrentZ,
    short TargetX,
    short TargetY,
    short TargetZ,
    bool IsMoving,
    sbyte DirectionX,
    sbyte DirectionY,
    ushort TicksRemaining
);

/// <summary>
/// Pacote de movimentação de NPCs para broadcast.
/// </summary>
[MemoryPackable]
public readonly partial record struct NpcMovementPacket(NpcMovementSnapshot[] Movements);
