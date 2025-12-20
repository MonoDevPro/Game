using Game.ECS.Navigation.Shared.Components;
using MemoryPack;

namespace Game.ECS.Navigation.Shared.Data;

/// <summary>
/// Snapshot de movimento para sincronização.
/// Enviado do servidor para o cliente.
/// </summary>
[MemoryPackable] public struct MovementSnapshot
{
    public int EntityId;
    public short CurrentX;
    public short CurrentY;
    public short TargetX;
    public short TargetY;
    public bool IsMoving;
    public MovementDirection Direction;
    public ushort TicksRemaining;

    public readonly float GetDurationSeconds(float tickRate)
        => TicksRemaining / tickRate;

    public readonly GridPosition CurrentPosition => new(CurrentX, CurrentY);
    public readonly GridPosition TargetPosition => new(TargetX, TargetY);
}

/// <summary>
/// Atualização em lote de múltiplas entidades.
/// </summary>
[MemoryPackable] public struct BatchMovementUpdate
{
    public long ServerTick;
    public MovementSnapshot[] Snapshots;
}

/// <summary>
/// Mensagem de teleporte instantâneo.
/// </summary>
[MemoryPackable] public struct TeleportMessage
{
    public int EntityId;
    public short X;
    public short Y;
    public MovementDirection FacingDirection;

    public readonly GridPosition Position => new(X, Y);
}

/// <summary>
/// Input de movimento do jogador.
/// </summary>
[MemoryPackable] public struct MoveInput
{
    public int SequenceId;
    public short TargetX;
    public short TargetY;
    public long ClientTimestamp;

    public readonly GridPosition Target => new(TargetX, TargetY);
}

/// <summary>
/// Confirmação de movimento do servidor.
/// </summary>
[MemoryPackable] public struct MoveConfirmation
{
    public int SequenceId;
    public bool Success;
    public short FinalX;
    public short FinalY;
    public long ServerTick;
}