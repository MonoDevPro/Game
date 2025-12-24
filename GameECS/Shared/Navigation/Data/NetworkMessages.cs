using GameECS.Shared.Navigation.Components;
using MemoryPack;

namespace GameECS.Shared.Navigation.Data;

/// <summary>
/// Dados de input de movimento enviados do cliente para o servidor.
/// </summary>
[MemoryPackable]
public readonly partial record struct MoveInputData(
    short TargetX,
    short TargetY,
    bool IsRunning);

/// <summary>
/// Snapshot de movimento para sincronização.
/// Enviado do servidor para o cliente.
/// </summary>
[MemoryPackable]
public readonly partial record struct MovementSnapshot(
    int EntityId,
    short CurrentX,
    short CurrentY,
    short TargetX,
    short TargetY,
    bool IsMoving,
    MovementDirection Direction,
    ushort TicksRemaining)
{

    public readonly float GetDurationSeconds(float tickRate)
        => TicksRemaining / tickRate;

    [MemoryPackIgnore]
    public GridPosition CurrentPosition => new(CurrentX, CurrentY);
    [MemoryPackIgnore]
    public GridPosition TargetPosition => new(TargetX, TargetY);
}

/// <summary>
/// Atualização em lote de múltiplas entidades.
/// </summary>
[MemoryPackable]
public partial struct BatchMovementUpdate
{
    public long ServerTick;
    public MovementSnapshot[] Snapshots;
}

/// <summary>
/// Mensagem de teleporte instantâneo.
/// </summary>
[MemoryPackable]
public partial struct TeleportMessage
{
    public int EntityId;
    public short X;
    public short Y;
    public MovementDirection FacingDirection;

    [MemoryPackIgnore]
    public readonly GridPosition Position => new(X, Y);
}
